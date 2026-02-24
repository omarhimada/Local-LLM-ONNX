using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
namespace OLLM.Memory;

public class Discussion {
	[VectorStoreKey] public long Id { get; set; }
	[VectorStoreData] public required string Text { get; set; }
	[VectorStoreVector(384)] public required GeneratedEmbeddings<Embedding<float>> Vector { get; set; }
	[VectorStoreData] public long UnixTimeMilliseconds { get; set; }

	public static Discussion FromDictionary(Dictionary<string, object?> d) => new() {
		Id = (long)d[nameof(Id)]!,
		Text = (string)d[nameof(Text)]!,
		Vector = (GeneratedEmbeddings<Embedding<float>>)d[nameof(Vector)]!,
		UnixTimeMilliseconds = (long)d[nameof(UnixTimeMilliseconds)]!
	};

	public Dictionary<string, object?> ToDictionary() => new() {
		{ nameof(Id), Id },
		{ nameof(Text), Text },
		{ nameof(Vector), Vector },
		{ nameof(UnixTimeMilliseconds), UnixTimeMilliseconds }
	};
}