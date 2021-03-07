using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Camp
{
	public class ActivityDefinitionJsonConverter : JsonConverter<ActivityDefinition>
	{
		private enum ReaderState
		{
			GetPropertyName,
			GetName,
			GetMinimumCapacity,
			GetOptimalCapacity,
			GetMaximumCapacity,
		}

		public override ActivityDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				throw new JsonException("Missing start of object");
			}
			var activityDefinition = new ActivityDefinition();
			ReaderState readerState = ReaderState.GetPropertyName;
			while (reader.Read())
			{
				switch (readerState)
				{
					case ReaderState.GetPropertyName:
						if (reader.TokenType == JsonTokenType.EndObject)
						{
							return activityDefinition;
						}
						if (reader.TokenType == JsonTokenType.PropertyName)
						{
							string propertyName = reader.GetString();
							switch (propertyName)
							{
								case nameof(activityDefinition.Name):
									readerState = ReaderState.GetName;
									break;
								case nameof(activityDefinition.MinimumCapacity):
									readerState = ReaderState.GetMinimumCapacity;
									break;
								case nameof(activityDefinition.MaximumCapacity):
									readerState = ReaderState.GetMaximumCapacity;
									break;
								case nameof(activityDefinition.OptimalCapacity):
									readerState = ReaderState.GetOptimalCapacity;
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

					case ReaderState.GetName:
						activityDefinition.Name = reader.GetString();
						readerState = ReaderState.GetPropertyName;
						break;

					case ReaderState.GetMinimumCapacity:
						activityDefinition.MinimumCapacity = reader.GetInt32();
						readerState = ReaderState.GetPropertyName;
						break;

					case ReaderState.GetOptimalCapacity:
						activityDefinition.OptimalCapacity = reader.GetInt32();
						readerState = ReaderState.GetPropertyName;
						break;

					case ReaderState.GetMaximumCapacity:
						activityDefinition.MaximumCapacity = reader.GetInt32();
						readerState = ReaderState.GetPropertyName;
						break;
				}

			}
			throw new JsonException();
		}

		public override void Write(Utf8JsonWriter writer, ActivityDefinition activityDefinition, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString(nameof(activityDefinition.Name), activityDefinition.Name);
			writer.WriteNumber(nameof(activityDefinition.MinimumCapacity), activityDefinition.MinimumCapacity);
			writer.WriteNumber(nameof(activityDefinition.OptimalCapacity), activityDefinition.OptimalCapacity);
			writer.WriteNumber(nameof(activityDefinition.MaximumCapacity), activityDefinition.MaximumCapacity);
			writer.WriteEndObject();
		}
	}
}
