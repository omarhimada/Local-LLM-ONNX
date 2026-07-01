namespace OLLM.Utility.J2CS.LexicalAndParsingModels.Jinja;
internal readonly record struct Token(TokenKind Kind, string Value, int Pos);
internal enum TokenKind {
	Text,
	Output,
	Tag,
	Eof
}