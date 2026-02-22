
using Microsoft.Extensions.AI;
using System.Text;
using static OLLM.Constants;
namespace OLLM.Utility.ModelSpecific;

internal static class Phi4Reasoning {
	public static string AsFormattedString(string? userPrompt) {
		if (string.IsNullOrEmpty(userPrompt)) {
			return string.Empty;
		}
		const string constructedRootSystemPrompt = $"{_defaultInstruction}";
		List<ChatMessage> messages = [
			new (ChatRole.System, constructedRootSystemPrompt),
			new (ChatRole.User, userPrompt)
		];
		StringBuilder sb = new();
		foreach (ChatMessage message in messages) {
			switch (message.Role.ToString()) {
				case _system:
					sb.Append($"{_imStart}{_system}{_imSep}{_firstTextContentOfChatMessageContents(message)}{_imEnd}");
					break;
				case _user:
					sb.Append($"{_imStart}{_user}{_imSep}{_firstTextContentOfChatMessageContents(message)}{_imEnd}");
					break;
			}
		}

		sb.Append($"{_imStart}{_assistant}{_imSep}");
		return sb.ToString();
	}
}