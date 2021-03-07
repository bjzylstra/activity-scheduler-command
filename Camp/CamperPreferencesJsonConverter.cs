using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Camp
{
	public class CamperPreferencesJsonConverter : JsonConverter<Dictionary<Camper,List<ActivityDefinition>>>
	{
		private enum ReaderState
		{
			GetStartTuple,
			GetCamperProperty,
			GetCamper,
			GetPreferencesProperty,
			GetPreferences,
			GetPreference,
			GetEndOfTuple,
		}

		public override Dictionary<Camper, List<ActivityDefinition>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartArray)
			{
				throw new JsonException("Missing start of array");
			}

			CamperJsonConverter camperConverter = new CamperJsonConverter();
			ActivityDefinitionJsonConverter activityConverter = new ActivityDefinitionJsonConverter();

			var preferences = new Dictionary<Camper, List<ActivityDefinition>>();
			Camper lastReadCamper = null;
			List<ActivityDefinition> lastReadPreferences = new List<ActivityDefinition>();
			ReaderState readerState = ReaderState.GetStartTuple;
			while (reader.Read())
			{
				switch (readerState)
				{
					case ReaderState.GetStartTuple:
						if (reader.TokenType == JsonTokenType.EndArray)
						{
							return preferences;
						}
						if (reader.TokenType != JsonTokenType.StartObject)
						{
							throw new JsonException("Missing start of camper preference object");
						}
						readerState = ReaderState.GetCamperProperty;
						break;
					case ReaderState.GetCamperProperty:
						if (reader.TokenType == JsonTokenType.PropertyName)
						{
							string propertyName = reader.GetString();
							if (propertyName.Equals("Camper"))
							{
								readerState = ReaderState.GetCamper;
							}
							else
							{
								throw new JsonException($"Unexpected property: {propertyName}");
							}
						}
						else
						{
							throw new JsonException("Expected property name");
						}
						break;
					case ReaderState.GetCamper:
						if (reader.TokenType != JsonTokenType.StartObject)
						{
							throw new JsonException("Missing start of camper preference object");
						}
						lastReadCamper = camperConverter.Read(ref reader, typeof(Camper), options);
						readerState = ReaderState.GetPreferencesProperty;
						break;
					case ReaderState.GetPreferencesProperty:
						if (reader.TokenType == JsonTokenType.PropertyName)
						{
							string propertyName = reader.GetString();
							if (propertyName.Equals("Preferences"))
							{
								readerState = ReaderState.GetPreferences;
							}
							else
							{
								throw new JsonException($"Unexpected property: {propertyName}");
							}
						}
						else
						{
							throw new JsonException("Expected property name");
						}
						break;
					case ReaderState.GetPreferences:
						if (reader.TokenType != JsonTokenType.StartArray)
						{
							throw new JsonException("Missing end of camper preference object");
						}
						readerState = ReaderState.GetPreference;
						break;
					case ReaderState.GetPreference:
						if (reader.TokenType == JsonTokenType.EndArray)
						{
							readerState = ReaderState.GetEndOfTuple;
						}
						else
						{
							lastReadPreferences.Add(activityConverter.Read(ref reader, typeof(ActivityDefinition), options));
						}
						break;
					case ReaderState.GetEndOfTuple:
						if (reader.TokenType != JsonTokenType.EndObject)
						{
							throw new JsonException("Missing end of camper preference object");
						}
						preferences.Add(lastReadCamper, lastReadPreferences);
						lastReadCamper = null;
						lastReadPreferences = new List<ActivityDefinition>();
						readerState = ReaderState.GetStartTuple;
						break;
				}
			}

			throw new JsonException();
		}

		public override void Write(Utf8JsonWriter writer, Dictionary<Camper, 
			List<ActivityDefinition>> camperPreferences, JsonSerializerOptions options)
		{
			CamperJsonConverter camperConverter = new CamperJsonConverter();
			ActivityDefinitionJsonConverter activityConverter = new ActivityDefinitionJsonConverter();
			writer.WriteStartArray();
			foreach (var keyValuePair in camperPreferences)
			{
				writer.WriteStartObject();
				writer.WritePropertyName("Camper");
				camperConverter.Write(writer, keyValuePair.Key, options);
				writer.WriteStartArray("Preferences");
				foreach (ActivityDefinition preference in keyValuePair.Value)
				{
					activityConverter.Write(writer, preference, options);
				}
				writer.WriteEndArray();
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
		}
	}
}
