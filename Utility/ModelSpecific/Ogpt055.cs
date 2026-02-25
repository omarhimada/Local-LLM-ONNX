using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace OLLM.Utility.ModelSpecific;

internal class Ogpt055 {
	#region Constants
	internal const string _analysisChannel = "<|channel|>analysis<|message|>";
	internal const string _any = "any";
	internal const string _anyArray = "any[]";
	internal const string _arguments = "arguments";
	internal const string _array = "array";
	internal const string _assignment = " = ";
	internal const string _assistant = "assistant";
	internal const string _boolean = "boolean";
	internal const string _booleanArray = "boolean[]";
	internal const string _braceClose = "}";
	internal const string _braceOpen = "{\n";
	internal const string _browser = "browser";
	internal const string _call = "<|call|>";
	internal const string _commaSeparator = ", ";
	internal const string _commentaryChannel = "<|channel|>commentary ";
	internal const string _content = "content";
	internal const string _contentType = "content_type";
	internal const string _currentDatePrefix = "Current date: ";
	internal const string _defaultIdentity = "You are ChatGPT, a large language model trained by OpenAI.";
	internal const string _developer = "developer";
	internal const string _emptyLambdaSuffix = "() => any;\n";
	internal const string _end = "<|end|>";
	internal const string _enum = "enum";
	internal const string _enumSeparator = "\" | \"";
	internal const string _finalChannel = "<|channel|>final<|message|>";
	internal const string _function = "function";
	internal const string _functionsCallNote = "\nCalls to these tools must go to the commentary channel: 'functions'.";
	internal const string _functionsNamespace = "functions";
	internal const string _headerPrefix = "## ";
	internal const string _instructionsHeader = "# Instructions\n\n";
	internal const string _integer = "integer";
	internal const string _items = "items";
	internal const string _json = "json";
	internal const string _knowledgeCutoff = "Knowledge cutoff: 2024-06";
	internal const string _lambdaAnySuffix = "}) => any;\n";
	internal const string _lambdaParam = "(_: {";
	internal const string _mediumEffort = "medium";
	internal const string _messageTag = "<|message|>";
	internal const string _name = "name";
	internal const string _namespaceKeyword = "namespace ";
	internal const string _namespaceOpen = " {\n";
	internal const string _newLine = "\n";
	internal const string _nullable = "nullable";
	internal const string _nullableSuffix = " | null";
	internal const string _number = "number";
	internal const string _numberArray = "number[]";
	internal const string _object = "object";
	internal const string _objectMatch = "object | object";
	internal const string _oneOf = "oneOf";
	internal const string _optionalFlag = "?";
	internal const string _parameters = "parameters";
	internal const string _properties = "properties";
	internal const string _propertySeparator = ": ";
	internal const string _python = "python";
	internal const string _quote = "\"";
	internal const string _reasoningPrefix = "Reasoning: ";
	internal const string _required = "required";
	internal const string _return = "<|return|>";
	internal const string _role = "role";
	internal const string _startAssistantAnalysis = "<|start|>assistant<|channel|>analysis<|message|>";
	internal const string _startAssistantFinal = "<|start|>assistant<|channel|>final<|message|>";
	internal const string _startAssistantGeneration = "<|start|>assistant\n";
	internal const string _startAssistantToFunctions = "<|start|>assistant to=functions.";
	internal const string _startDeveloper = "<|start|>developer<|message|>";
	internal const string _startFunctionsToAssistant = "<|start|>functions.";
	internal const string _startSystemMessage = "<|start|>system<|message|>";
	internal const string _startUser = "<|start|>user<|message|>";
	internal const string _string = "string";
	internal const string _stringArray = "string[]";
	internal const string _system = "system";
	internal const string _thinkEnd = "</think>";
	internal const string _thinkStart = "<think>";
	internal const string _thinking = "thinking";
	internal const string _toAssistantCommentary = " to=assistant<|channel|>commentary<|message|>";
	internal const string _tool = "tool";
	internal const string _toolCalls = "tool_calls";
	internal const string _toolsHeader = "# Tools\n\n";
	internal const string _type = "type";
	internal const string _typeKeyword = "type ";
	internal const string _unionSeparator = " | ";
	internal const string _user = "user";
	internal const string _validChannelsNote = "# Valid channels: analysis, commentary, final. Channel must be included for every message.";
	internal const string _dateformat = "yyyy-MM-dd";
	#endregion
	internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

	internal static string RenderTemplate(ChatMessage[] messages,
		List<string>? builtinTools = null, string? modelIdentity = null,
		string? reasoningEffort = null, bool addGenerationPrompt = false) {

		StringBuilder construct = new();

		construct.Append(_startSystemMessage);
		construct.Append(BuildSystemMessage(modelIdentity, reasoningEffort, builtinTools));

		string? developerMessage = string.Empty;

		int startIndex = 0;
		if (messages.Length > 0) {
			string? firstRole = messages[0].Role.ToString();
			if (firstRole is _developer or _system) {
				developerMessage = messages[0].Text;
				startIndex = 1;
			}
		}

		#region Think first then pick up a hammer and fix something. Until then, no tools.
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
		#endregion

		string? lastToolCallName = null;

		for (int i = startIndex; i < messages.Length; i++) {
			ChatMessage message = messages[i];
			string? role = messages[i].Role.ToString();
			bool isLast = (i == messages.Length - 1);

			if (role == _assistant) {
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
					lastToolCallName = null;
				}
			} else if (role == _tool) {
				string? content = message.Text;
				construct.Append($"{_startFunctionsToAssistant}{lastToolCallName}{_toAssistantCommentary}{JsonSerializer.Serialize(content, JsonOptions)}{_end}");
			} else if (role == _user) {
				string? content = message.Text;
				construct.Append($"{_startUser}{content}{_end}");
			}
		}

		if (addGenerationPrompt) {
			construct.Append(_startAssistantGeneration);
		}

		return construct.ToString();
	}

	internal string RenderTypeScriptType(JsonElement paramSpec, HashSet<string> requiredParams, bool isNullable = false) {
		StringBuilder sb = new();
		bool hasType = paramSpec.TryGetProperty(_type, out JsonElement typeProp);

		if (hasType && typeProp.ValueKind == JsonValueKind.String && typeProp.GetString() == _array) {
			if (paramSpec.TryGetProperty(_items, out JsonElement itemsProp) && itemsProp.ValueKind != JsonValueKind.Null) {
				if (itemsProp.TryGetProperty(_type, out JsonElement itemTypeProp) && itemTypeProp.ValueKind == JsonValueKind.String) {
					string? itemType = itemTypeProp.GetString();
					switch (itemType) {
						case _string:
							sb.Append(_stringArray);
							break;
						case _number:
						case _integer:
							sb.Append(_numberArray);
							break;
						case _boolean:
							sb.Append(_booleanArray);
							break;
						default:
							string inner = RenderTypeScriptType(itemsProp, requiredParams);
							sb.Append(inner == _objectMatch || inner.Length > 50 ? _anyArray : $"{inner}[]");
							break;
					}
				} else {
					string inner = RenderTypeScriptType(itemsProp, requiredParams);
					sb.Append(inner == _objectMatch || inner.Length > 50 ? _anyArray : $"{inner}[]");
				}
			} else {
				sb.Append(_anyArray);
			}

			if (GetNullable(paramSpec) || isNullable)
				sb.Append(_nullableSuffix);
		} else if (hasType && typeProp.ValueKind == JsonValueKind.Array && typeProp.GetArrayLength() > 0) {
			sb.Append(string.Join(_unionSeparator, typeProp.EnumerateArray().Select(t => t.GetString())));
		} else if (paramSpec.TryGetProperty(_oneOf, out JsonElement oneOfProp) && oneOfProp.ValueKind == JsonValueKind.Array) {
			bool hasObjectVariants = oneOfProp.EnumerateArray().Any(v => v.TryGetProperty(_type, out JsonElement t) && t.GetString() == _object);
			if (hasObjectVariants && oneOfProp.GetArrayLength() > 1) {
				sb.Append(_any);
			} else {
				sb.Append(string.Join(_unionSeparator, oneOfProp.EnumerateArray().Select(v => RenderTypeScriptType(v, requiredParams))));
			}
		} else if (hasType && typeProp.ValueKind == JsonValueKind.String) {
			string typeStr = typeProp.GetString()!;
			switch (typeStr) {
				case _string:
					if (paramSpec.TryGetProperty(_enum, out JsonElement enumProp) && enumProp.ValueKind == JsonValueKind.Array) {
						sb.Append(_quote).Append(string.Join(_enumSeparator, enumProp.EnumerateArray().Select(e => e.GetString()))).Append(_quote);
					} else {
						sb.Append(_string);
						if (GetNullable(paramSpec) || isNullable)
							sb.Append(_nullableSuffix);
					}
					break;
				case _number:
				case _integer:
					sb.Append(_number);
					break;
				case _boolean:
					sb.Append(_boolean);
					break;
				case _object:
					if (paramSpec.TryGetProperty(_properties, out JsonElement propsProp) && propsProp.ValueKind == JsonValueKind.Object) {
						sb.Append(_braceOpen);
						HashSet<string> reqList = [];
						if (paramSpec.TryGetProperty(_required, out JsonElement reqProp) && reqProp.ValueKind == JsonValueKind.Array) {
							JsonElement.ArrayEnumerator reqPropEnumerated = reqProp.EnumerateArray();
							IEnumerable<string?> reqPropEnumeratedStrings = reqPropEnumerated.Select(r => r.GetString());
							IEnumerable<string> reqPropEnumeratedNonNullStrings = reqPropEnumeratedStrings.Where(s => s != null)!;

							reqList = [.. reqPropEnumeratedNonNullStrings];
						}

						List<JsonProperty> props = propsProp.EnumerateObject().ToList();
						for (int i = 0; i < props.Count; i++) {
							JsonProperty prop = props[i];
							sb.Append(prop.Name);
							if (!reqList.Contains(prop.Name))
								sb.Append(_optionalFlag);
							sb.Append(_propertySeparator).Append(RenderTypeScriptType(prop.Value, reqList));
							if (i < props.Count - 1)
								sb.Append(_commaSeparator);
						}
						sb.Append(_braceClose);
					} else {
						sb.Append(_object);
					}
					break;
				default:
					sb.Append(_any);
					break;
			}
		} else {
			sb.Append(_any);
		}

		return sb.ToString();
	}

	#region We must give them tools, instead of ccalling them tools. TODO 
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

	internal static string BuildSystemMessage(string? modelIdentity, string? reasoningEffort, List<string>? builtinTools) {
		StringBuilder sb = new();

		sb.AppendLine(string.IsNullOrEmpty(modelIdentity) ? _defaultIdentity : modelIdentity);
		sb.AppendLine(_knowledgeCutoff);
		sb.Append(_currentDatePrefix).AppendLine(DateTime.Now.ToString(_dateformat)).AppendLine();
		sb.Append(_reasoningPrefix).Append(reasoningEffort ?? _mediumEffort).AppendLine("\n");
		sb.Append(_validChannelsNote);

		//if (tools.HasValue && tools.Value.ValueKind == JsonValueKind.Array && tools.Value.GetArrayLength() > 0) {
		//	sb.Append(_functionsCallNote);
		//}

		return sb.ToString();
	}

	private bool GetNullable(JsonElement paramSpec) =>
		paramSpec.TryGetProperty(_nullable, out JsonElement prop) && prop.ValueKind == JsonValueKind.True;

	private string? GetMessageText(ChatMessage message) => message.Text;
}