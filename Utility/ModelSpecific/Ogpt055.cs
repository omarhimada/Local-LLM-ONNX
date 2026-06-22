using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace OLLM.Utility.ModelSpecific;

using static Constants;
using static System.Windows.Forms.Design.AxImporter;

internal class Deepseek {
	internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

	internal static string RenderTemplate(ChatMessage[] messages) {
		return BuildPrompt(messages[1].Text);
	}

	private static string BuildPrompt(string userMessage) {
		return $"""
        <|system|>
        {_defaultInstruction}
        <|user|>
        {userMessage}
        <|assistant|>
        """;
	}

	#region For client integrations
	//internal string RenderToolNamespace(string namespaceName, JsonElement tools) {
	//	StringBuilder sb = new();
	//	sb.Append(_headerPrefix).Append(namespaceName).Append("\n\n");
	//	sb.Append(_namespaceKeyword).Append(namespaceName).Append(_namespaceOpen);

	//	foreach (JsonElement toolWrap in tools.EnumerateArray()) {
	//		JsonElement tool = toolWrap.GetProperty(_function);
	//		sb.Append(_typeKeyword).Append(GetMessageText(tool, _name)).Append(_assignment);

	//		if (tool.TryGetProperty(_parameters, out JsonElement parameters) &&
	//			parameters.TryGetProperty(_properties, out JsonElement properties)) {
	//			sb.AppendLine(_lambdaParam);
	//			HashSet<string> reqList = [];

	//			if (parameters.TryGetProperty(_required, out JsonElement reqProp) && reqProp.ValueKind == JsonValueKind.Array) {
	//				reqList = [.. reqProp.EnumerateArray().Select(r => r.GetString()).Where(s => s != null)!];
	//			}

	//			foreach (JsonProperty prop in properties.EnumerateObject()) {
	//				sb.Append(prop.Name);
	//				if (!reqList.Contains(prop.Name))
	//					sb.Append(_optionalFlag);
	//				sb.Append(_propertySeparator).Append(RenderTypeScriptType(prop.Value, reqList)).AppendLine(",");
	//			}
	//			sb.AppendLine(_lambdaAnySuffix);
	//		} else {
	//			sb.AppendLine(_emptyLambdaSuffix);
	//		}
	//	}
	//	sb.AppendLine(_braceClose);
	//	return sb.ToString();
	//}
	#endregion
}