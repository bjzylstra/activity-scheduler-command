using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Camp
{
    /// <summary>
    /// Custom serializer for a camper that only uses the name.
    /// </summary>
    public class CamperJsonConverter : JsonConverter<Camper>
    {
        private enum ReaderState
        {
            GetPropertyName,
            GetFirstName,
            GetLastName
        }
  
        public override Camper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Missing start of object");
            }
            var camper = new Camper();
			ReaderState readerState = ReaderState.GetPropertyName;
			while (reader.Read())
			{
				switch (readerState)
				{
					case ReaderState.GetPropertyName:
						if (reader.TokenType == JsonTokenType.EndObject)
						{
							return camper;
						}
						if (reader.TokenType == JsonTokenType.PropertyName)
						{
							string propertyName = reader.GetString();
							switch (propertyName)
							{
								case nameof(camper.FirstName):
									readerState = ReaderState.GetFirstName;
									break;
								case nameof(camper.LastName):
									readerState = ReaderState.GetLastName;
									break;
								default:
									throw new JsonException($"Unknown property: {propertyName}");
							}
						}
						else
						{
							throw new JsonException("Expected property name");
						}
						break;

					case ReaderState.GetFirstName:
						camper.FirstName = reader.GetString();
						readerState = ReaderState.GetPropertyName;
						break;

					case ReaderState.GetLastName:
						camper.LastName = reader.GetString();
						readerState = ReaderState.GetPropertyName;
						break;
				}
			}
			throw new JsonException();
		}

		public override void Write(Utf8JsonWriter writer, Camper camper, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (!String.IsNullOrEmpty(camper.FirstName))
            {
                writer.WriteString(nameof(camper.FirstName), camper.FirstName);
            }
            writer.WriteString(nameof(camper.LastName), camper.LastName);
            writer.WriteEndObject();
        }
    }
}
