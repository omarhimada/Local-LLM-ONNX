using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace OLLM.Utility.ModelSpecific;

using static Constants;
internal class Ogpt055 {
	internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

	internal static string RenderTemplate(ChatMessage[] messages,
		List<string>? builtinTools = null,
		string? reasoningEffort = null, bool addGenerationPrompt = false) {

		StringBuilder construct = new();

		construct.Append(_startSystemMessage);

		#region For potential client integrations
		//bool hasTools =
		//	tools.HasValue &&
		//	tools.Value.ValueKind == JsonValueKind.Array &&
		//	tools.Value.GetArrayLength() > 0;

		//if (!string.IsNullOrEmpty(developerMessage) || hasTsools) {
		//	construct.Append(_startDeveloper);

		//	if (!string.IsNullOrEmpty(developerMessage)) {
		//		construct.Append(_instructionsHeader).Append(developerMessage).Append("\n\n");
		//	}

		//	if (hasTools) {
		//		construct.Append(_toolsHeader);
		//		construct.Append(RenderToolNamespace(_functionsNamespace, tools!.Value));
		//	}
		//	construct.Append(_end);
		//}
		//string? lastToolCallName;
		#endregion

		for (int i = 0; i < messages.Length; i++) {
			ChatMessage message = messages[i];
			ChatRole role = messages[i].Role;
			bool isLast = (i == messages.Length - 1);

			if (role == ChatRole.System) {
				construct.Append(message.Text);
				construct.Append(_end);
			}

			if (role == ChatRole.Assistant) {
				string content = message.Text;
				string thought = string.Empty;
				if (message.Text.Contains(_thinkStart) && message.Text.Contains(_thinkEnd)) {
					message.Text.Split(_thinkStart)[1].Split(_thinkEnd)[0] = content;
					thought = content;
				}

				#region Optimization: Channel validation
				//if ((content?.Contains(_analysisChannel) == true || content?.Contains(_finalChannel) == true) ||
				//	(thinking?.Contains(_analysisChannel) == true || thinking?.Contains(_finalChannel) == true)) {
				//	throw new InvalidOperationException("You have passed a message containing <|channel|> tags in the content or thinking field.");
				//}

				/*if (message.AdditionalProperties?.TryGetValue(_toolCalls, out JsonElement toolCalls) && toolCalls.ValueKind == JsonValueKind.Array && toolCalls.GetArrayLength() > 0) {
					// Optimization: Check for future final message once if needed
					bool futureFinalFound = false;
					for (int j = i + 1; j < messages.Length; j++) {
						if (messages[j].Role.ToString() == _assistant && !messages[j].Contents.Any(ai => ai.) {
							futureFinalFound = true;
							break;
						}
						//

						//if (messages[j].Role.ToString() == _assistant) &&
						//	!messages[j].TryGetProperty(_toolCalls, out _)) {

						//	 = true;
						//	break;
						//}
					}

					JsonElement toolCall = toolCalls[0];
					if (toolCall.TryGetProperty(_function, out JsonElement funcProp)) {
						toolCall = funcProp;
					}

					if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(thinking)) {
						throw new InvalidOperationException("Cannot pass both content and thinking in an assistant message with tool calls!");
					}

					if (!futureFinalFound) {
						if (!string.IsNullOrEmpty(content)) {
							construct.Append($"{_startAssistantAnalysis}{content}{_end}");
						} else if (!string.IsNullOrEmpty(thinking)) {
							construct.Append($"{_startAssistantAnalysis}{thinking}{_end}");
						}
					}

					string? tcName = GetMessageText(toolCall, _name);
					string cType = GetMessageText(toolCall, _contentType) ?? _json;

					construct.Append($"{_startAssistantToFunctions}{tcName}{_commentaryChannel}{cType}{_messageTag}");
					construct.Append(JsonSerializer.Serialize(toolCall.GetProperty(_arguments), JsonOptions));
					construct.Append(_call);

					lastToolCallName = tcName;
				} else*/
				#endregion

				if (isLast && !addGenerationPrompt) {
					if (!string.IsNullOrEmpty(thought)) {
						construct.Append($"{_startAssistantAnalysis}{thought}{_end}");
					}
					construct.Append($"{_startAssistantFinal}{content}{_return}");
				} else {
					construct.Append($"{_startAssistantFinal}{content}{_end}");
					//lastToolCallName = null;
				}
			} else if (role == ChatRole.Tool) {
				#region Client integrations
				//string? content = message.Text;
				//construct.Append($"{_startFunctionsToAssistant}{lastToolCallName}{_toAssistantCommentary}{JsonSerializer.Serialize(content, JsonOptions)}{_end}");
				#endregion
			} else if (role == ChatRole.User) {
				string? content = message.Text;
				construct.Append($"{_startUser}{content}{_end}");
			}
		}

		if (addGenerationPrompt) {
			construct.Append(_startAssistantGeneration);
		}

		return construct.ToString();
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