using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqliteVec;
using OLLM.Utility;
using System.Windows;

namespace OLLM.Memory;

using static Constants;

internal class Remember : IDisposable {
#if DEBUG
	// If debugging you will continuously erase the memories.db due to rebuilding the solution erasing the /bin/Debug/
	// So, keep the memories.db in the Windows user's home directory instead.
	protected static string _db = $"Data Source={Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\memories.db";
#else
		// If published/building in Release mode, the memories.db will be beside the executable
		protected static string _db = $"Data Source={Environment.ProcessPath}\\..\\memories.db";
#endif
	internal const string _dbDiscussions = "discussions";
	//internal SqliteVectorStore? _vectorStore;
	internal SqliteDynamicCollection _memoriesCollection;
	internal static MiniEmbedder? _embedder;
	private readonly CancellationTokenSource _cts = new();
	private readonly SqliteCollectionOptions _sqliteOptions = new() {
		VectorVirtualTableName = "Recollections",
		EmbeddingGenerator = _embedder
	};

	//private readonly SqliteVectorStoreOptions _sqliteVectorStoreOptions = new();

	internal Remember(MiniEmbedder embeddingGenerator) {
		CancellationToken ct = _cts.Token;
		_memoriesCollection = new(_db, _memoriesDbName, _sqliteOptions);
		Task memoryInitializationTask = Task.Run(async () => {
			await StartAsync(embeddingGenerator, ct);
		}, ct);
		Task.WaitAll(memoryInitializationTask);
	}

	internal async Task StartAsync(MiniEmbedder embedder, CancellationToken ct = default) {
		_embedder = embedder;
		//_vectorStore?.Dispose();
		//_vectorStore = new(_db, _sqliteVectorStoreOptions);
		await _memoriesCollection.EnsureCollectionExistsAsync(ct);
	}

	/// <summary>
	/// Store a discussion that had occurred.
	/// </summary>
	internal async Task MemorizeDiscussionAsync(string? text, CancellationToken ct = default) {
		if (text is null) {
			return;
		}
		if (_memoriesCollection is not null && _embedder is not null && !string.IsNullOrEmpty(_embedder.EmbedderState.VocabularyPath)) {
			try {
				string cleanedString = StringCleaner.Md(text);
				GeneratedEmbeddings<Embedding<float>> vector =
					await _embedder.GenerateAsync(
						[cleanedString],
						null,
						cancellationToken: ct);
				long id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				Discussion turn = new() {
					Id = id,
					Text = text,
					Vector = vector,
					UnixTimeMilliseconds = id
				};
				await _memoriesCollection.UpsertAsync(turn.ToDictionary(), ct);
			} catch (Exception exception) {
				System.Windows.MessageBox.Show(exception.Message);
			}
		}
	}

	/// <summary>
	/// Try to remember before responding. It is possible to forget, so this method can return null.
	/// </summary>
	internal async Task<IReadOnlyList<Discussion>?> RememberDiscussionsAsync(
		string query,
		int topK = 8,
		int candidates = 33,
		double halfLifeDays = 365,
		CancellationToken ct = default) {
		if (_memoriesCollection is not null && _embedder is not null) {
			ReadOnlyMemory<float> embeddingVectorQuery =
				await _embedder.GenerateVectorAsync(query, cancellationToken: ct);
			// Retrieve candidate set from vector search (bigger than topK)
			List<(Discussion Turn, double AdjustedDistance)> scored = new(candidates);
			long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			// When VectorSearchResult score is null fallback to ordering sequentially with an index 'rank'
			int rank = 0;
			await foreach (VectorSearchResult<Dictionary<string, object?>> hit in _memoriesCollection.SearchAsync(embeddingVectorQuery, top: candidates, cancellationToken: ct)) {
				Discussion turn = Discussion.FromDictionary(hit.Record);
				// With VectorSearchResult the hit.Score is distance, so lower is better
				double baseDistance = hit.Score ?? rank;
				double adjustedDistance = baseDistance;
				if (!double.IsPositiveInfinity(halfLifeDays) && halfLifeDays > 0) {
					double ageDays = Math.Max(0, (nowMs - turn.UnixTimeMilliseconds) / 86_400_000.0);
					double decay = Math.Exp(-Math.Log(2) * ageDays / halfLifeDays);
					// Older memories become effectively 'more distant'
					adjustedDistance = baseDistance / Math.Max(decay, 1e-9);
				}
				scored.Add((turn, adjustedDistance));
				rank++;
			}
			// 'scored' is filtered such that 'OrderBy' time complexity O(NlogN) doesn't get out of control
			return scored
				.OrderBy(x => x.AdjustedDistance)
				.Take(topK)
				.Select(x => x.Turn)
				.ToList();
		}
		return null;
	}

	/// <summary>
	/// Close the connections, keep the memories.
	/// </summary>
	public void Dispose() {
		_memoriesCollection?.Dispose();
		//_vectorStore?.Dispose();
		//_vectorStore = null;
	}
}