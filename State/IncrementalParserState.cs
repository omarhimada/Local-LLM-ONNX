using System.Windows.Documents;

namespace OLLM.State;

internal class IncrementalParserState {
	public bool InCodeBlock { get; set; } = false;
	public Paragraph CurrentCodeParagraph { get; set; } = null!;
}