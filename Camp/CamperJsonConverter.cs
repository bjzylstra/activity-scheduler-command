using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Camp
{
	/// <summary>
	/// Custom serializer for a camper that only uses the name.
	/// </summary>
	public class CamperJsonConverter : JsonConverter<Camper>
	{
		public override Camper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			string fullName = reader.GetString();
			string[] nameParts = fullName.Split(',');
			return new Camper
			{
				FirstName = nameParts.Length > 1 ? nameParts[1] : String.Empty,
				LastName = nameParts[0]
			};
		}

		public override void Write(Utf8JsonWriter writer, Camper camper, JsonSerializerOptions options)
		{
			if (String.IsNullOrEmpty(camper.FirstName))
			{
				writer.WriteStringValue($"{camper.LastName}");
			}
			else
			{
				writer.WriteStringValue($"{camper.LastName},{camper.FirstName}");
			}
		}
	}
}
