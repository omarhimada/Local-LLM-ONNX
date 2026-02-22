using System;
using System.Collections.Generic;
using System.Text;

namespace OLLM.Utility;

public static class Base64e {
	/// <summary>
	/// Encodes the provided input string to its Base64 representation.
	/// </summary> <param name="input">The string to encode.</param>
	/// <returns>A Base64 encoded string.</returns><exception cref="ArgumentNullException">Thrown when the input is null.</exception>
	public static string EncodeToBase64(string input) {
		if (input == null)
			throw new ArgumentNullException(nameof(input), "Input cannot be null.");
		// Convert the input string to bytes using UTF8 encoding.
		byte[] bytes = Encoding.UTF8.GetBytes(input);
		// Convert the byte array to a Base64 encoded string.
		return Convert.ToBase64String(bytes);
	}

	/// <summary>
	/// Decodes the provided Base64 string back to its original plain text.
	/// </summary>
	/// <param name="base64Input">The Base64 string to decode.</param>
	/// <returns>
	/// The decoded plain text string.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the input is null.</exception>
	/// <exception cref="ArgumentException">Thrown when the input contains invalid Base64 characters.</exception>
	public static string DecodeFromBase64(string base64Input) {
		if (base64Input == null)
			throw new ArgumentNullException(nameof(base64Input), "Input cannot be null.");
		try {
			// Convert the Base64 string back to a byte array.
			byte[] bytes = Convert.FromBase64String(base64Input);
			// Convert the byte array back to a UTF8 string.
			return Encoding.UTF8.GetString(bytes);
		} catch (FormatException) {
			// If the string is not valid Base64, raise an error.
			throw new ArgumentException("Invalid Base64 input provided.", nameof(base64Input));
		}
	}
}