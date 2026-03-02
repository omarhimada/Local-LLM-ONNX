namespace OLLM.Utility;

public abstract record FdBlockMd;
public record LineBreakMdBlock() : FdBlockMd;
public record ParagraphFdBlockMd(List<InlineSpan> Inlines, int HeadingLevel = 0) : FdBlockMd;
public record CodeFdBlockMd(string Code, string? Lang) : FdBlockMd;
public record BulletListFdBlockMd(List<List<InlineSpan>> Items) : FdBlockMd;
public abstract record InlineSpan;
public record TextSpan(string Text) : InlineSpan;
public record CodeSpan(string Text) : InlineSpan;
public record BoldSpan(List<InlineSpan> Children) : InlineSpan;
public record ItalicSpan(List<InlineSpan> Children) : InlineSpan;