using System.Text.RegularExpressions;

namespace OLLM.Utility.Syntax;

public static partial class LanguageRegistry {
	internal const string _fencePlain = "plain";
	internal const string _fenceTxt = "txt";
	internal const string _fenceCSharp = "csharp";
	internal const string _fenceCs = "cs";
	internal const string _fenceJson = "json";
	internal const string _fenceJs = "js";
	internal const string _fenceJavaScript = "javascript";
	internal const string _fenceC = "c";
	internal const string _fenceH = "h";
	internal const string _fenceCpp = "cpp";
	internal const string _fenceCxx = "cxx";
	internal const string _fenceCc = "cc";
	internal const string _fenceHpp = "hpp";

	internal const string _rxPlainString = @"(""[^""]*""|'[^']*')";
	internal const string _rxPlainNumber = @"\b\d+(\.\d+)?\b";
	internal const string _rxPlainIdentifier = @"\b[_A-Za-z]\w*\b";

	internal const string _rxCSharpCommentLine = @"//.*?$";
	internal const string _rxCSharpCommentBlock = @"/\*.*?\*/";
	internal const string _rxCSharpPreprocessor = @"^\s*#(if|elif|else|endif|define|undef|region|endregion|nullable|pragma|error|warning|line)\b.*?$";
	internal const string _rxCSharpStringVerbatim = @"@""(""|[^""])*""";
	internal const string _rxCSharpStringInterpolatedVerbatim = @"\$@""(""|[^""])*""";
	internal const string _rxCSharpStringVerbatimInterpolated = @"@\$""(""|[^""])*""";
	internal const string _rxCSharpString = @"""(\\.|[^""\\])*""";
	internal const string _rxCSharpChar = @"'(\\.|[^'\\])'";
	internal const string _rxCSharpHexNumber = @"\b0x[0-9a-fA-F_]+\b";
	internal const string _rxCSharpNumber = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[mMfFdD]?\b";
	internal const string _rxCSharpAttribute = @"\[\s*[_A-ZaZ]\w*(\.[_A-ZaZ]\w*)*\s*(\(|\])";
	internal const string _rxCSharpIdentifier = @"\b[_A-Za-z]\w*\b";
	internal const string _rxCSharpOperator = @"(\+\+|--|=>|==|!=|<=|>=|&&|\|\||<<|>>|\+=|-=|\*=|/=|%=|&=|\|=|\^=|<<=|>>=)";
	internal const string _rxCSharpPunctuation = @"[{}\[\]();,\.]";

	internal const string _rxJsCommentLine = @"//.*?$";
	internal const string _rxJsCommentBlock = @"/\*.*?\*/";
	internal const string _rxJsStringDouble = @"""(\\.|[^""\\])*""";
	internal const string _rxJsStringSingle = @"'(\\.|[^'\\])*'";
	internal const string _rxJsTemplateString = @"`([^`\\]|\\.|(\$\{[\s\S]*?\}))*`";
	internal const string _rxJsNumber = @"\b-?\d+(\.\d+)?([eE][+-]?\d+)?\b";
	internal const string _rxJsIdentifier = @"\b[_A-Za-z]\w*\b";
	internal const string _rxJsOperator = @"(===|!==|==|!=|<=|>=|\+\+|--|=>|\|\||&&)";
	internal const string _rxJsPunctuation = @"[{}\[\]();,\.]";

	internal const string _rxJsonString = @"""(\\.|[^""\\])*""";
	internal const string _rxJsonNumber = @"\b-?\d+(\.\d+)?([eE][+-]?\d+)?\b";
	internal const string _rxJsonLiteral = @"\b(true|false|null)\b";
	internal const string _rxJsonPunctuation = @"[{}\[\]:,]";

	internal const string _rxCCommentLine = @"//.*?$";
	internal const string _rxCCommentBlock = @"/\*.*?\*/";
	internal const string _rxCPreprocessor = @"^\s*#(include|define|undef|if|ifdef|ifndef|elif|else|endif|pragma|error|line)\b.*?$";
	internal const string _rxCString = @"L?""(\\.|[^""\\])*""";
	internal const string _rxCChar = @"L?'(\\.|[^'\\])'";
	internal const string _rxCHexNumber = @"\b0x[0-9a-fA-F]+\b";
	internal const string _rxCNumber = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[uUlLfF]?\b";
	internal const string _rxCIdentifier = @"\b[_A-Za-z]\w*\b";
	internal const string _rxCOperator = @"(\+\+|--|->|==|!=|<=|>=|&&|\|\||<<|>>|\+=|-=|\*=|/=|%=|&=|\|=|\^=|<<=|>>=)";
	internal const string _rxCPunctuation = @"[{}\[\]();,\.]";

	internal const string _rxCppCommentLine = @"//.*?$";
	internal const string _rxCppCommentBlock = @"/\*.*?\*/";
	internal const string _rxCppPreprocessor = @"^\s*#(include|define|undef|if|ifdef|ifndef|elif|else|endif|pragma|error|line)\b.*?$";
	internal const string _rxCppRawString = @"R""\([^\)]*\)""";
	internal const string _rxCppString = @"u8?L?""(\\.|[^""\\])*""";
	internal const string _rxCppChar = @"L?'(\\.|[^'\\])'";
	internal const string _rxCppHexNumber = @"\b0x[0-9a-fA-F]+\b";
	internal const string _rxCppNumber = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[uUlLfF]*\b";
	internal const string _rxCppIdentifier = @"\b[_A-ZaZ]\w*\b";
	internal const string _rxCppOperator = @"(\+\+|--|->|::|==|!=|<=|>=|&&|\|\||<<|>>|<<=|>>=|\+=|-=|\*=|/=|%=|&=|\|=|\^=)";
	internal const string _rxCppPunctuation = @"[{}\[\]();,\.<>]";

	internal const string _kwCSharpAbstract = "abstract";
	internal const string _kwCSharpAs = "as";
	internal const string _kwCSharpBase = "base";
	internal const string _kwCSharpBool = "bool";
	internal const string _kwCSharpBreak = "break";
	internal const string _kwCSharpByte = "byte";
	internal const string _kwCSharpCase = "case";
	internal const string _kwCSharpCatch = "catch";
	internal const string _kwCSharpChar = "char";
	internal const string _kwCSharpChecked = "checked";
	internal const string _kwCSharpClass = "class";
	internal const string _kwCSharpConst = "const";
	internal const string _kwCSharpContinue = "continue";
	internal const string _kwCSharpDecimal = "decimal";
	internal const string _kwCSharpDefault = "default";
	internal const string _kwCSharpDelegate = "delegate";
	internal const string _kwCSharpDo = "do";
	internal const string _kwCSharpDouble = "double";
	internal const string _kwCSharpElse = "else";
	internal const string _kwCSharpEnum = "enum";
	internal const string _kwCSharpEvent = "event";
	internal const string _kwCSharpExplicit = "explicit";
	internal const string _kwCSharpExtern = "extern";
	internal const string _kwCSharpFalse = "false";
	internal const string _kwCSharpFinally = "finally";
	internal const string _kwCSharpFixed = "fixed";
	internal const string _kwCSharpFloat = "float";
	internal const string _kwCSharpFor = "for";
	internal const string _kwCSharpForeach = "foreach";
	internal const string _kwCSharpGoto = "goto";
	internal const string _kwCSharpIf = "if";
	internal const string _kwCSharpImplicit = "implicit";
	internal const string _kwCSharpIn = "in";
	internal const string _kwCSharpInt = "int";
	internal const string _kwCSharpInterface = "interface";
	internal const string _kwCSharpInternal = "internal";
	internal const string _kwCSharpIs = "is";
	internal const string _kwCSharpLock = "lock";
	internal const string _kwCSharpLong = "long";
	internal const string _kwCSharpNamespace = "namespace";
	internal const string _kwCSharpNew = "new";
	internal const string _kwCSharpNull = "null";
	internal const string _kwCSharpObject = "object";
	internal const string _kwCSharpOperator = "operator";
	internal const string _kwCSharpOut = "out";
	internal const string _kwCSharpOverride = "override";
	internal const string _kwCSharpParams = "params";
	internal const string _kwCSharpPrivate = "private";
	internal const string _kwCSharpProtected = "protected";
	internal const string _kwCSharpPublic = "public";
	internal const string _kwCSharpReadonly = "readonly";
	internal const string _kwCSharpRef = "ref";
	internal const string _kwCSharpReturn = "return";
	internal const string _kwCSharpSbyte = "sbyte";
	internal const string _kwCSharpSealed = "sealed";
	internal const string _kwCSharpShort = "short";
	internal const string _kwCSharpSizeof = "sizeof";
	internal const string _kwCSharpStackalloc = "stackalloc";
	internal const string _kwCSharpStatic = "static";
	internal const string _kwCSharpString = "string";
	internal const string _kwCSharpStruct = "struct";
	internal const string _kwCSharpSwitch = "switch";
	internal const string _kwCSharpThis = "this";
	internal const string _kwCSharpThrow = "throw";
	internal const string _kwCSharpTrue = "true";
	internal const string _kwCSharpTry = "try";
	internal const string _kwCSharpTypeof = "typeof";
	internal const string _kwCSharpUint = "uint";
	internal const string _kwCSharpUlong = "ulong";
	internal const string _kwCSharpUnchecked = "unchecked";
	internal const string _kwCSharpUnsafe = "unsafe";
	internal const string _kwCSharpUshort = "ushort";
	internal const string _kwCSharpUsing = "using";
	internal const string _kwCSharpVirtual = "virtual";
	internal const string _kwCSharpVoid = "void";
	internal const string _kwCSharpVolatile = "volatile";
	internal const string _kwCSharpWhile = "while";
	internal const string _kwCSharpVar = "var";
	internal const string _kwCSharpRecord = "record";
	internal const string _kwCSharpInit = "init";
	internal const string _kwCSharpWith = "with";
	internal const string _kwCSharpWhen = "when";
	internal const string _kwCSharpWhere = "where";
	internal const string _kwCSharpSelect = "select";
	internal const string _kwCSharpFrom = "from";
	internal const string _kwCSharpYield = "yield";
	internal const string _kwCSharpAsync = "async";
	internal const string _kwCSharpAwait = "await";

	internal const string _typeCSharpString = "String";
	internal const string _typeCSharpInt32 = "Int32";
	internal const string _typeCSharpInt64 = "Int64";
	internal const string _typeCSharpBoolean = "Boolean";
	internal const string _typeCSharpObject = "Object";
	internal const string _typeCSharpTask = "Task";
	internal const string _typeCSharpValueTask = "ValueTask";
	internal const string _typeCSharpList = "List";
	internal const string _typeCSharpDictionary = "Dictionary";
	internal const string _typeCSharpSpan = "Span";
	internal const string _typeCSharpReadOnlySpan = "ReadOnlySpan";
	internal const string _typeCSharpGuid = "Guid";
	internal const string _typeCSharpDateTime = "DateTime";
	internal const string _typeCSharpDateTimeOffset = "DateTimeOffset";
	internal const string _typeCSharpTimeSpan = "TimeSpan";
	internal const string _typeCSharpCancellationToken = "CancellationToken";

	internal const string _kwJsBreak = "break";
	internal const string _kwJsCase = "case";
	internal const string _kwJsCatch = "catch";
	internal const string _kwJsClass = "class";
	internal const string _kwJsConst = "const";
	internal const string _kwJsContinue = "continue";
	internal const string _kwJsDebugger = "debugger";
	internal const string _kwJsDefault = "default";
	internal const string _kwJsDelete = "delete";
	internal const string _kwJsDo = "do";
	internal const string _kwJsElse = "else";
	internal const string _kwJsExport = "export";
	internal const string _kwJsExtends = "extends";
	internal const string _kwJsFinally = "finally";
	internal const string _kwJsFor = "for";
	internal const string _kwJsFunction = "function";
	internal const string _kwJsIf = "if";
	internal const string _kwJsImport = "import";
	internal const string _kwJsIn = "in";
	internal const string _kwJsInstanceof = "instanceof";
	internal const string _kwJsLet = "let";
	internal const string _kwJsNew = "new";
	internal const string _kwJsReturn = "return";
	internal const string _kwJsSuper = "super";
	internal const string _kwJsSwitch = "switch";
	internal const string _kwJsThis = "this";
	internal const string _kwJsThrow = "throw";
	internal const string _kwJsTry = "try";
	internal const string _kwJsTypeof = "typeof";
	internal const string _kwJsVar = "var";
	internal const string _kwJsVoid = "void";
	internal const string _kwJsWhile = "while";
	internal const string _kwJsWith = "with";
	internal const string _kwJsYield = "yield";
	internal const string _kwJsTrue = "true";
	internal const string _kwJsFalse = "false";
	internal const string _kwJsNull = "null";
	internal const string _kwJsUndefined = "undefined";
	internal const string _kwJsAwait = "await";
	internal const string _kwJsAsync = "async";

	internal const string _kwJsonTrue = "true";
	internal const string _kwJsonFalse = "false";
	internal const string _kwJsonNull = "null";

	internal const string _kwCAuto = "auto";
	internal const string _kwCBreak = "break";
	internal const string _kwCCase = "case";
	internal const string _kwCChar = "char";
	internal const string _kwCConst = "const";
	internal const string _kwCContinue = "continue";
	internal const string _kwCDefault = "default";
	internal const string _kwCDo = "do";
	internal const string _kwCDouble = "double";
	internal const string _kwCElse = "else";
	internal const string _kwCEnum = "enum";
	internal const string _kwCExtern = "extern";
	internal const string _kwCFloat = "float";
	internal const string _kwCFor = "for";
	internal const string _kwCGoto = "goto";
	internal const string _kwCIf = "if";
	internal const string _kwCInline = "inline";
	internal const string _kwCInt = "int";
	internal const string _kwCLong = "long";
	internal const string _kwCRegister = "register";
	internal const string _kwCRestrict = "restrict";
	internal const string _kwCReturn = "return";
	internal const string _kwCShort = "short";
	internal const string _kwCSigned = "signed";
	internal const string _kwCSizeof = "sizeof";
	internal const string _kwCStatic = "static";
	internal const string _kwCStruct = "struct";
	internal const string _kwCSwitch = "switch";
	internal const string _kwCTypedef = "typedef";
	internal const string _kwCUnion = "union";
	internal const string _kwCUnsigned = "unsigned";
	internal const string _kwCVoid = "void";
	internal const string _kwCVolatile = "volatile";
	internal const string _kwCWhile = "while";
	internal const string _kwCAlignas = "_Alignas";
	internal const string _kwCAlignof = "_Alignof";
	internal const string _kwCAtomic = "_Atomic";
	internal const string _kwCBool = "_Bool";
	internal const string _kwCComplex = "_Complex";
	internal const string _kwCGeneric = "_Generic";
	internal const string _kwCImaginary = "_Imaginary";
	internal const string _kwCNoreturn = "_Noreturn";
	internal const string _kwCStaticAssert = "_Static_assert";
	internal const string _kwCThreadLocal = "_Thread_local";

	internal const string _typeCSizeT = "size_t";
	internal const string _typeCPtrdiffT = "ptrdiff_t";
	internal const string _typeCFile = "FILE";
	internal const string _typeCTimeT = "time_t";
	internal const string _typeCClockT = "clock_t";

	internal const string _kwCppAlignas = "alignas";
	internal const string _kwCppAlignof = "alignof";
	internal const string _kwCppAnd = "and";
	internal const string _kwCppAndEq = "and_eq";
	internal const string _kwCppAsm = "asm";
	internal const string _kwCppAuto = "auto";
	internal const string _kwCppBitand = "bitand";
	internal const string _kwCppBitor = "bitor";
	internal const string _kwCppBool = "bool";
	internal const string _kwCppBreak = "break";
	internal const string _kwCppCase = "case";
	internal const string _kwCppCatch = "catch";
	internal const string _kwCppChar = "char";
	internal const string _kwCppChar8T = "char8_t";
	internal const string _kwCppChar16T = "char16_t";
	internal const string _kwCppChar32T = "char32_t";
	internal const string _kwCppClass = "class";
	internal const string _kwCppCompl = "compl";
	internal const string _kwCppConcept = "concept";
	internal const string _kwCppConst = "const";
	internal const string _kwCppConsteval = "consteval";
	internal const string _kwCppConstexpr = "constexpr";
	internal const string _kwCppConstinit = "constinit";
	internal const string _kwCppConstCast = "const_cast";
	internal const string _kwCppContinue = "continue";
	internal const string _kwCppCoAwait = "co_await";
	internal const string _kwCppCoReturn = "co_return";
	internal const string _kwCppCoYield = "co_yield";
	internal const string _kwCppDecltype = "decltype";
	internal const string _kwCppDefault = "default";
	internal const string _kwCppDelete = "delete";
	internal const string _kwCppDo = "do";
	internal const string _kwCppDouble = "double";
	internal const string _kwCppDynamicCast = "dynamic_cast";
	internal const string _kwCppElse = "else";
	internal const string _kwCppEnum = "enum";
	internal const string _kwCppExplicit = "explicit";
	internal const string _kwCppExport = "export";
	internal const string _kwCppExtern = "extern";
	internal const string _kwCppFalse = "false";
	internal const string _kwCppFloat = "float";
	internal const string _kwCppFor = "for";
	internal const string _kwCppFriend = "friend";
	internal const string _kwCppGoto = "goto";
	internal const string _kwCppIf = "if";
	internal const string _kwCppInline = "inline";
	internal const string _kwCppInt = "int";
	internal const string _kwCppLong = "long";
	internal const string _kwCppMutable = "mutable";
	internal const string _kwCppNamespace = "namespace";
	internal const string _kwCppNew = "new";
	internal const string _kwCppNoexcept = "noexcept";
	internal const string _kwCppNot = "not";
	internal const string _kwCppNotEq = "not_eq";
	internal const string _kwCppNullptr = "nullptr";
	internal const string _kwCppOperator = "operator";
	internal const string _kwCppOr = "or";
	internal const string _kwCppOrEq = "or_eq";
	internal const string _kwCppPrivate = "private";
	internal const string _kwCppProtected = "protected";
	internal const string _kwCppPublic = "public";
	internal const string _kwCppRegister = "register";
	internal const string _kwCppReinterpretCast = "reinterpret_cast";
	internal const string _kwCppRequires = "requires";
	internal const string _kwCppReturn = "return";
	internal const string _kwCppShort = "short";
	internal const string _kwCppSigned = "signed";
	internal const string _kwCppSizeof = "sizeof";
	internal const string _kwCppStatic = "static";
	internal const string _kwCppStaticAssert = "static_assert";
	internal const string _kwCppStaticCast = "static_cast";
	internal const string _kwCppStruct = "struct";
	internal const string _kwCppSwitch = "switch";
	internal const string _kwCppTemplate = "template";
	internal const string _kwCppThis = "this";
	internal const string _kwCppThreadLocal = "thread_local";
	internal const string _kwCppThrow = "throw";
	internal const string _kwCppTrue = "true";
	internal const string _kwCppTry = "try";
	internal const string _kwCppTypedef = "typedef";
	internal const string _kwCppTypeid = "typeid";
	internal const string _kwCppTypename = "typename";
	internal const string _kwCppUnion = "union";
	internal const string _kwCppUnsigned = "unsigned";
	internal const string _kwCppUsing = "using";
	internal const string _kwCppVirtual = "virtual";
	internal const string _kwCppVoid = "void";
	internal const string _kwCppVolatile = "volatile";
	internal const string _kwCppWcharT = "wchar_t";
	internal const string _kwCppWhile = "while";
	internal const string _kwCppXor = "xor";
	internal const string _kwCppXorEq = "xor_eq";

	internal const string _typeCppStd = "std";
	internal const string _typeCppString = "string";
	internal const string _typeCppVector = "vector";
	internal const string _typeCppMap = "map";
	internal const string _typeCppUnorderedMap = "unordered_map";
	internal const string _typeCppUniquePtr = "unique_ptr";
	internal const string _typeCppSharedPtr = "shared_ptr";
	internal const string _typeCppOptional = "optional";
	internal const string _typeCppVariant = "variant";
	internal const string _typeCppTuple = "tuple";
	internal const string _typeCppPair = "pair";
	internal const string _typeCppSizeT = "size_t";
	internal const string _typeCppInt32T = "int32_t";
	internal const string _typeCppInt64T = "int64_t";
	internal const string _typeCppUint32T = "uint32_t";
	internal const string _typeCppUint64T = "uint64_t";

	private static readonly Dictionary<string, LanguageDefinition> _byFence =
		new(StringComparer.OrdinalIgnoreCase);

	public static void Register(string fenceName, LanguageDefinition lang) =>
		_byFence[fenceName] = lang;

	public static LanguageDefinition Resolve(string? fenceName) {
		if (fenceName is not null && _byFence.TryGetValue(fenceName.Trim(), out LanguageDefinition? found))
			return found;

		// fallback
		return _byFence.TryGetValue(_fencePlain, out LanguageDefinition? plain) ? plain : BuildPlain();
	}

	public static void RegisterDefaults() {
		Register(_fencePlain, BuildPlain());
		Register(_fenceTxt, BuildPlain());

		Register(_fenceCSharp, BuildCSharp());
		Register(_fenceCs, Resolve(_fenceCSharp));

		Register(_fenceJson, BuildJson());
		Register(_fenceC, BuildC());
		Register(_fenceH, Resolve(_fenceC));

		Register(_fenceCpp, BuildCpp());
		Register(_fenceCxx, Resolve(_fenceCpp));
		Register(_fenceCc, Resolve(_fenceCpp));
		Register(_fenceHpp, Resolve(_fenceCpp));

		Register(_fenceJs, BuildJavaScriptLike(_fenceJavaScript));
		Register(_fenceJavaScript, Resolve(_fenceJs));
	}

	private static LanguageDefinition BuildPlain() {
		LanguageDefinition lang = new(_fencePlain);
		lang.Rules.Add(new(TokenKind.String, PlainStringRegex(), Priority: 1));
		lang.Rules.Add(new(TokenKind.Number, PlainNumberRegex(), Priority: 1));
		lang.Rules.Add(new(TokenKind.Identifier, PlainIdentifierRegex(), Priority: 0));
		return lang;
	}

	private static Regex Rx(string pattern) => new(pattern, RegexOptions.Compiled | RegexOptions.Multiline);

	private static Regex RxSingleLine(string pattern) => new(pattern, RegexOptions.Compiled | RegexOptions.Singleline);

	[GeneratedRegex(_rxPlainString, RegexOptions.Compiled)]
	private static partial Regex PlainStringRegex();
	[GeneratedRegex(_rxPlainNumber, RegexOptions.Compiled)]
	private static partial Regex PlainNumberRegex();
	[GeneratedRegex(_rxPlainIdentifier, RegexOptions.Compiled)]
	private static partial Regex PlainIdentifierRegex();

	private static LanguageDefinition BuildCSharp() {
		LanguageDefinition lang = new(_fenceCSharp);

		lang.Keywords.UnionWith([
			_kwCSharpAbstract,_kwCSharpAs,_kwCSharpBase,_kwCSharpBool,_kwCSharpBreak,_kwCSharpByte,_kwCSharpCase,_kwCSharpCatch,_kwCSharpChar,_kwCSharpChecked,
			_kwCSharpClass,_kwCSharpConst,_kwCSharpContinue,_kwCSharpDecimal,_kwCSharpDefault,_kwCSharpDelegate,_kwCSharpDo,_kwCSharpDouble,_kwCSharpElse,
			_kwCSharpEnum,_kwCSharpEvent,_kwCSharpExplicit,_kwCSharpExtern,_kwCSharpFalse,_kwCSharpFinally,_kwCSharpFixed,_kwCSharpFloat,_kwCSharpFor,
			_kwCSharpForeach,_kwCSharpGoto,_kwCSharpIf,_kwCSharpImplicit,_kwCSharpIn,_kwCSharpInt,_kwCSharpInterface,_kwCSharpInternal,_kwCSharpIs,_kwCSharpLock,
			_kwCSharpLong,_kwCSharpNamespace,_kwCSharpNew,_kwCSharpNull,_kwCSharpObject,_kwCSharpOperator,_kwCSharpOut,_kwCSharpOverride,_kwCSharpParams,
			_kwCSharpPrivate,_kwCSharpProtected,_kwCSharpPublic,_kwCSharpReadonly,_kwCSharpRef,_kwCSharpReturn,_kwCSharpSbyte,_kwCSharpSealed,_kwCSharpShort,
			_kwCSharpSizeof,_kwCSharpStackalloc,_kwCSharpStatic,_kwCSharpString,_kwCSharpStruct,_kwCSharpSwitch,_kwCSharpThis,_kwCSharpThrow,_kwCSharpTrue,
			_kwCSharpTry,_kwCSharpTypeof,_kwCSharpUint,_kwCSharpUlong,_kwCSharpUnchecked,_kwCSharpUnsafe,_kwCSharpUshort,_kwCSharpUsing,_kwCSharpVirtual,
			_kwCSharpVoid,_kwCSharpVolatile,_kwCSharpWhile,_kwCSharpVar,_kwCSharpRecord,_kwCSharpInit,_kwCSharpWith,_kwCSharpWhen,_kwCSharpWhere,_kwCSharpSelect,
			_kwCSharpFrom,_kwCSharpYield,_kwCSharpAsync,_kwCSharpAwait
		]);

		lang.Types.UnionWith([
			_typeCSharpString,_typeCSharpInt32,_typeCSharpInt64,_typeCSharpBoolean,_typeCSharpObject,_typeCSharpTask,_typeCSharpValueTask,_typeCSharpList,_typeCSharpDictionary,
			_typeCSharpSpan,_typeCSharpReadOnlySpan,_typeCSharpGuid,_typeCSharpDateTime,_typeCSharpDateTimeOffset,_typeCSharpTimeSpan,_typeCSharpCancellationToken
		]);

		lang.Rules.Add(new(TokenKind.Comment, RxSingleLine(_rxCSharpCommentLine), Priority: 10));
		lang.Rules.Add(new(TokenKind.Comment, RxSingleLine(_rxCSharpCommentBlock), Priority: 10));
		lang.Rules.Add(new(TokenKind.Preprocessor, Rx(_rxCSharpPreprocessor), Priority: 9));

		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxCSharpStringVerbatim), Priority: 9));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxCSharpStringInterpolatedVerbatim), Priority: 9));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxCSharpStringVerbatimInterpolated), Priority: 9));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxCSharpString), Priority: 8));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxCSharpChar), Priority: 8));

		lang.Rules.Add(new(TokenKind.Number, Rx(_rxCSharpHexNumber), Priority: 7));
		lang.Rules.Add(new(TokenKind.Number, Rx(_rxCSharpNumber), Priority: 7));

		lang.Rules.Add(new(TokenKind.Attribute, Rx(_rxCSharpAttribute), Priority: 6));

		lang.Rules.Add(new(TokenKind.Identifier, Rx(_rxCSharpIdentifier), Priority: 3));

		lang.Rules.Add(new(TokenKind.Operator, Rx(_rxCSharpOperator), Priority: 2));
		lang.Rules.Add(new(TokenKind.Punctuation, Rx(_rxCSharpPunctuation), Priority: 1));

		return lang;
	}

	private static LanguageDefinition BuildJavaScriptLike(string name) {
		LanguageDefinition lang = new(name);
		lang.Keywords.UnionWith([
			_kwJsBreak,_kwJsCase,_kwJsCatch,_kwJsClass,_kwJsConst,_kwJsContinue,_kwJsDebugger,_kwJsDefault,_kwJsDelete,_kwJsDo,
			_kwJsElse,_kwJsExport,_kwJsExtends,_kwJsFinally,_kwJsFor,_kwJsFunction,_kwJsIf,_kwJsImport,_kwJsIn,_kwJsInstanceof,
			_kwJsLet,_kwJsNew,_kwJsReturn,_kwJsSuper,_kwJsSwitch,_kwJsThis,_kwJsThrow,_kwJsTry,_kwJsTypeof,_kwJsVar,_kwJsVoid,
			_kwJsWhile,_kwJsWith,_kwJsYield,_kwJsTrue,_kwJsFalse,_kwJsNull,_kwJsUndefined,_kwJsAwait,_kwJsAsync
		]);

		lang.Rules.Add(new(TokenKind.Comment, RxSingleLine(_rxJsCommentLine), Priority: 10));
		lang.Rules.Add(new(TokenKind.Comment, RxSingleLine(_rxJsCommentBlock), Priority: 10));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxJsStringDouble), Priority: 9));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxJsStringSingle), Priority: 9));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxJsTemplateString), Priority: 9));
		lang.Rules.Add(new(TokenKind.Number, Rx(_rxJsNumber), Priority: 8));
		lang.Rules.Add(new(TokenKind.Identifier, Rx(_rxJsIdentifier), Priority: 3));
		lang.Rules.Add(new(TokenKind.Operator, Rx(_rxJsOperator), Priority: 2));
		lang.Rules.Add(new(TokenKind.Punctuation, Rx(_rxJsPunctuation), Priority: 1));
		return lang;
	}

	private static LanguageDefinition BuildJson() {
		LanguageDefinition lang = new(_fenceJson);
		lang.Keywords.UnionWith([_kwJsonTrue, _kwJsonFalse, _kwJsonNull]);

		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxJsonString), Priority: 10));
		lang.Rules.Add(new(TokenKind.Number, Rx(_rxJsonNumber), Priority: 9));
		lang.Rules.Add(new(TokenKind.Identifier, Rx(_rxJsonLiteral), Priority: 8));
		lang.Rules.Add(new(TokenKind.Punctuation, Rx(_rxJsonPunctuation), Priority: 1));
		return lang;
	}

	private static LanguageDefinition BuildC() {
		LanguageDefinition lang = new(_fenceC);

		lang.Keywords.UnionWith([
			_kwCAuto,_kwCBreak,_kwCCase,_kwCChar,_kwCConst,_kwCContinue,_kwCDefault,_kwCDo,_kwCDouble,
			_kwCElse,_kwCEnum,_kwCExtern,_kwCFloat,_kwCFor,_kwCGoto,_kwCIf,_kwCInline,_kwCInt,_kwCLong,
			_kwCRegister,_kwCRestrict,_kwCReturn,_kwCShort,_kwCSigned,_kwCSizeof,_kwCStatic,_kwCStruct,
			_kwCSwitch,_kwCTypedef,_kwCUnion,_kwCUnsigned,_kwCVoid,_kwCVolatile,_kwCWhile,_kwCAlignas,
			_kwCAlignof,_kwCAtomic,_kwCBool,_kwCComplex,_kwCGeneric,_kwCImaginary,_kwCNoreturn,_kwCStaticAssert,_kwCThreadLocal
		]);

		lang.Types.UnionWith([
			_typeCSizeT,_typeCPtrdiffT,_typeCFile,_typeCTimeT,_typeCClockT
		]);

		lang.Rules.Add(new(TokenKind.Comment, RxSingleLine(_rxCCommentLine), Priority: 10));
		lang.Rules.Add(new(TokenKind.Comment, RxSingleLine(_rxCCommentBlock), Priority: 10));
		lang.Rules.Add(new(TokenKind.Preprocessor, Rx(_rxCPreprocessor), Priority: 9));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxCString), Priority: 8));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxCChar), Priority: 8));
		lang.Rules.Add(new(TokenKind.Number, Rx(_rxCHexNumber), Priority: 7));
		lang.Rules.Add(new(TokenKind.Number, Rx(_rxCNumber), Priority: 7));

		lang.Rules.Add(new(TokenKind.Identifier, Rx(_rxCIdentifier), Priority: 3));

		lang.Rules.Add(new(TokenKind.Operator, Rx(_rxCOperator), Priority: 2));

		lang.Rules.Add(new(TokenKind.Punctuation, Rx(_rxCPunctuation), Priority: 1));

		return lang;
	}

	private static LanguageDefinition BuildCpp() {
		LanguageDefinition lang = new(_fenceCpp);

		lang.Keywords.UnionWith([
			_kwCppAlignas,_kwCppAlignof,_kwCppAnd,_kwCppAndEq,_kwCppAsm,_kwCppAuto,_kwCppBitand,_kwCppBitor,_kwCppBool,
			_kwCppBreak,_kwCppCase,_kwCppCatch,_kwCppChar,_kwCppChar8T,_kwCppChar16T,_kwCppChar32T,_kwCppClass,
			_kwCppCompl,_kwCppConcept,_kwCppConst,_kwCppConsteval,_kwCppConstexpr,_kwCppConstinit,_kwCppConstCast,
			_kwCppContinue,_kwCppCoAwait,_kwCppCoReturn,_kwCppCoYield,_kwCppDecltype,_kwCppDefault,_kwCppDelete,
			_kwCppDo,_kwCppDouble,_kwCppDynamicCast,_kwCppElse,_kwCppEnum,_kwCppExplicit,_kwCppExport,_kwCppExtern,
			_kwCppFalse,_kwCppFloat,_kwCppFor,_kwCppFriend,_kwCppGoto,_kwCppIf,_kwCppInline,_kwCppInt,_kwCppLong,_kwCppMutable,
			_kwCppNamespace,_kwCppNew,_kwCppNoexcept,_kwCppNot,_kwCppNotEq,_kwCppNullptr,_kwCppOperator,_kwCppOr,_kwCppOrEq,
			_kwCppPrivate,_kwCppProtected,_kwCppPublic,_kwCppRegister,_kwCppReinterpretCast,_kwCppRequires,_kwCppReturn,_kwCppShort,
			_kwCppSigned,_kwCppSizeof,_kwCppStatic,_kwCppStaticAssert,_kwCppStaticCast,_kwCppStruct,_kwCppSwitch,_kwCppTemplate,
			_kwCppThis,_kwCppThreadLocal,_kwCppThrow,_kwCppTrue,_kwCppTry,_kwCppTypedef,_kwCppTypeid,_kwCppTypename,_kwCppUnion,_kwCppUnsigned,
			_kwCppUsing,_kwCppVirtual,_kwCppVoid,_kwCppVolatile,_kwCppWcharT,_kwCppWhile,_kwCppXor,_kwCppXorEq
		]);

		lang.Types.UnionWith([
			_typeCppStd,_typeCppString,_typeCppVector,_typeCppMap,_typeCppUnorderedMap,_typeCppUniquePtr,_typeCppSharedPtr,
			_typeCppOptional,_typeCppVariant,_typeCppTuple,_typeCppPair,_typeCppSizeT,_typeCppInt32T,_typeCppInt64T,
			_typeCppUint32T,_typeCppUint64T
		]);

		lang.Rules.Add(new(TokenKind.Comment, RxSingleLine(_rxCppCommentLine), Priority: 10));
		lang.Rules.Add(new(TokenKind.Comment, RxSingleLine(_rxCppCommentBlock), Priority: 10));
		lang.Rules.Add(new(TokenKind.Preprocessor, Rx(_rxCppPreprocessor), Priority: 9));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxCppRawString), Priority: 9));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxCppString), Priority: 8));
		lang.Rules.Add(new(TokenKind.String, RxSingleLine(_rxCppChar), Priority: 8));
		lang.Rules.Add(new(TokenKind.Number, Rx(_rxCppHexNumber), Priority: 7));
		lang.Rules.Add(new(TokenKind.Number, Rx(_rxCppNumber), Priority: 7));
		lang.Rules.Add(new(TokenKind.Identifier, Rx(_rxCppIdentifier), Priority: 3));
		lang.Rules.Add(new(TokenKind.Operator, Rx(_rxCppOperator), Priority: 2));
		lang.Rules.Add(new(TokenKind.Punctuation, Rx(_rxCppPunctuation), Priority: 1));

		return lang;
	}

}
