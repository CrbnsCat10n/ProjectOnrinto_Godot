// 在 JSON 与 Godot Vector2 之间进行序列化和反序列化转换。
using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Onrinto.Chart;

public class Vector2Converter : JsonConverter<Vector2>
{
	// 从 JSON 对象读取 Vector2。
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        float x = 0;
        float y = 0;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("预期一个对象来解析 Vector2");

		// 遍历对象属性并读取坐标字段。
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new Vector2(x, y);

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()?.ToLower();
                reader.Read();

                if (propertyName == "x") x = (float)reader.GetDouble();
                else if (propertyName == "y") y = (float)reader.GetDouble();
            }
        }

        return new Vector2(x, y);
    }

	// 将 Vector2 写入 JSON 对象。
    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}
