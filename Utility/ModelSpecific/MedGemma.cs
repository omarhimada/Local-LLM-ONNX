namespace OLLM.Utility.ModelSpecific;

using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Constants;

internal class MedGemma27B {
	internal static string RenderTemplate(
		ChatMessage[] messages) {
		//List<string> builtinTools = null,
		//string reasoningEffort = null) {

		if (messages == null || messages.Length == 0)
			throw new ArgumentException(_userFriendlyMessagesError, nameof(messages));

		StringBuilder sb = new StringBuilder();
		sb.Append(_bosToken);

		string firstUserPrefix = string.Empty;
		IEnumerable<ChatMessage> loopMessages = messages;

		ChatRole role = messages[0].Role;
		if (role == ChatRole.System) {
			firstUserPrefix = $"{ExtractSystemText(messages[0])}{_nl}{_nl}";
			loopMessages = messages.Skip(1);
		}

		ChatMessage[] loopArray = loopMessages.ToArray();

		for (int i = 0; i < loopArray.Length; i++) {
			ChatMessage message = loopArray[i];
			role = message.Role;
			bool expectedUser = i % 2 == 0;

			if ((role == ChatRole.User) != expectedUser) {
				throw new InvalidOperationException(_userFriendlyExpectedRoleTurnsError);
			}

			sb.Append(_startOfTurn)
			  .Append(role)
			  .Append(_nl);

			if (i == 0)
				sb.Append(firstUserPrefix);

			sb.Append(RenderContent(message.Contents));
			sb.Append(_endOfTurn);
		}

		// Equivalent to add_generation_prompt == true.
		sb.Append(_startOfTurn)
		  .Append(_modelRole)
		  .Append(_nl);

		return sb.ToString();
	}

	private static string ExtractSystemText(dynamic content) {

		if (content is KernelContent) {
			if (content.InnerContent is string text)
				return text.Trim();
		}

		if (content is ChatMessageContentItemCollection items)
			return items.FirstOrDefault()?.InnerContent?.ToString() ?? string.Empty;

		throw new InvalidOperationException(_userFriendlyInvalidContentError);
	}

	private static string RenderContent(object content) {
		if (content is string text)
			return text.Trim();

		if (content is IEnumerable<ChatMessageContent> items) {
			StringBuilder sb = new StringBuilder();

			foreach (ChatMessageContent item in items) {
				switch (item.MimeType) {
					case _image:
						sb.Append(_startOfImage);
						break;

					case _text:
						sb.Append(ExtractSystemText(item.InnerContent ?? string.Empty));
						break;
				}
			}

			return sb.ToString();
		}

		throw new InvalidOperationException(_userFriendlyInvalidContentError);
	}
}