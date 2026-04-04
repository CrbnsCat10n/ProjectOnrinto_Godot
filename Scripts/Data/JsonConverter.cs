using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

//unread...

namespace Onrinto.Chart;

public class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 预设默认值
        float x = 0;
        float y = 0;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("预期一个对象来解析 Vector2");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new Vector2(x, y);

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()?.ToLower(); // 统一转小写处理
                reader.Read();

                if (propertyName == "x") x = (float)reader.GetDouble();
                else if (propertyName == "y") y = (float)reader.GetDouble();
            }
        }

        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}