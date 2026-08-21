using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IcarusProfileMod;

internal sealed class UePropertyTag
{
    public UePropertyTag(string name, string typeName)
    {
        Name = name;
        TypeName = typeName;
    }

    public string Name { get; set; }
    public string TypeName { get; set; }
    public object? Value { get; set; }
    public string? InnerType { get; set; }
    public string? StructType { get; set; }
    public string? EnumType { get; set; }
    public string? ElementName { get; set; }
    public List<UePropertyTag> Nested { get; } = [];

    public UePropertyTag? Find(string name)
    {
        return Nested.FirstOrDefault(property =>
            string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed class UePropertySerializer
{
    private static readonly Dictionary<string, int> FixedStructSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Vector"] = 12,
        ["Vector2D"] = 8,
        ["Rotator"] = 12,
        ["Quat"] = 16,
        ["LinearColor"] = 16,
        ["Color"] = 4,
        ["Guid"] = 16,
        ["DateTime"] = 8,
        ["Timespan"] = 8
    };

    public List<UePropertyTag> Deserialize(byte[] data)
    {
        using BinaryReader reader = new(new MemoryStream(data), Encoding.UTF8, leaveOpen: false);
        return ReadProperties(reader, data.Length);
    }

    public byte[] Serialize(IReadOnlyList<UePropertyTag> properties)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
        WriteProperties(writer, properties, addTrailingZeros: true);
        return stream.ToArray();
    }

    public static UePropertyTag? FindProperty(IEnumerable<UePropertyTag> properties, string name)
    {
        foreach (UePropertyTag property in properties)
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property;
            }

            UePropertyTag? nested = FindProperty(property.Nested, name);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static List<UePropertyTag> ReadProperties(BinaryReader reader, long endPosition)
    {
        List<UePropertyTag> properties = [];
        while (reader.BaseStream.Position < endPosition && reader.BaseStream.Length - reader.BaseStream.Position >= 4)
        {
            UePropertyTag? property = ReadProperty(reader);
            if (property is null || string.Equals(property.Name, "None", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            properties.Add(property);
        }

        return properties;
    }

    private static UePropertyTag? ReadProperty(BinaryReader reader)
    {
        string? name = ReadFString(reader);
        if (string.IsNullOrEmpty(name) || string.Equals(name, "None", StringComparison.OrdinalIgnoreCase))
        {
            return new UePropertyTag("None", "terminator");
        }

        string typeName = ReadFString(reader) ?? "";
        int size = reader.ReadInt32();
        _ = reader.ReadInt32();
        UePropertyTag property = new(name, typeName);

        switch (typeName)
        {
            case "ArrayProperty":
                property.InnerType = ReadFString(reader);
                _ = reader.ReadByte();
                ReadArrayValue(reader, property, size);
                break;
            case "StructProperty":
                property.StructType = ReadFString(reader);
                _ = reader.ReadBytes(17);
                ReadStructValue(reader, property, size);
                break;
            case "EnumProperty":
                property.EnumType = ReadFString(reader);
                _ = reader.ReadByte();
                property.Value = ReadFString(reader);
                break;
            case "BoolProperty":
                property.Value = reader.ReadByte() != 0;
                _ = reader.ReadByte();
                break;
            default:
                _ = reader.ReadByte();
                ReadSimpleValue(reader, property, typeName, size);
                break;
        }

        return property;
    }

    private static UePropertyTag? ReadPropertyTag(BinaryReader reader)
    {
        string? name = ReadFString(reader);
        if (string.IsNullOrEmpty(name) || string.Equals(name, "None", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string typeName = ReadFString(reader) ?? "";
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        UePropertyTag property = new(name, typeName);
        if (typeName == "StructProperty")
        {
            property.StructType = ReadFString(reader);
            _ = reader.ReadBytes(16);
        }
        else if (typeName == "ArrayProperty")
        {
            property.InnerType = ReadFString(reader);
        }
        else if (typeName == "EnumProperty")
        {
            property.EnumType = ReadFString(reader);
        }

        _ = reader.ReadByte();
        return property;
    }

    private static void ReadArrayValue(BinaryReader reader, UePropertyTag property, int size)
    {
        long valueStart = reader.BaseStream.Position;
        long valueEnd = valueStart + size;
        int count = reader.ReadInt32();
        if (string.Equals(property.InnerType, "StructProperty", StringComparison.OrdinalIgnoreCase))
        {
            UePropertyTag? prototype = ReadPropertyTag(reader);
            if (prototype is not null)
            {
                property.ElementName = prototype.Name;
                property.StructType = prototype.StructType;
                for (int i = 0; i < count; i++)
                {
                    UePropertyTag element = new(prototype.Name, "StructProperty")
                    {
                        StructType = prototype.StructType
                    };
                    element.Nested.AddRange(ReadProperties(reader, valueEnd));
                    property.Nested.Add(element);
                }
            }
        }
        else if (string.Equals(property.InnerType, "ByteProperty", StringComparison.OrdinalIgnoreCase))
        {
            property.Value = reader.ReadBytes(count).ToArray();
        }

        if (reader.BaseStream.Position < valueEnd)
        {
            reader.BaseStream.Position = valueEnd;
        }
    }

    private static void ReadStructValue(BinaryReader reader, UePropertyTag property, int size)
    {
        if (property.StructType is not null && FixedStructSizes.TryGetValue(property.StructType, out int fixedSize))
        {
            property.Value = reader.ReadBytes(fixedSize);
            return;
        }

        long endPosition = reader.BaseStream.Position + size;
        property.Nested.AddRange(ReadProperties(reader, endPosition));
        if (reader.BaseStream.Position < endPosition)
        {
            reader.BaseStream.Position = endPosition;
        }
    }

    private static void ReadSimpleValue(BinaryReader reader, UePropertyTag property, string typeName, int size)
    {
        property.Value = typeName switch
        {
            "IntProperty" => reader.ReadInt32(),
            "UInt32Property" => reader.ReadUInt32(),
            "Int64Property" => reader.ReadInt64(),
            "FloatProperty" => reader.ReadSingle(),
            "DoubleProperty" => reader.ReadDouble(),
            "StrProperty" or "NameProperty" => ReadFString(reader),
            _ => reader.ReadBytes(size)
        };
    }

    private static void WriteProperties(BinaryWriter writer, IReadOnlyList<UePropertyTag> properties, bool addTrailingZeros = false)
    {
        foreach (UePropertyTag property in properties)
        {
            WriteProperty(writer, property);
        }

        WriteFString(writer, "None");
        if (addTrailingZeros)
        {
            writer.Write(0);
        }
    }

    private static void WriteProperty(BinaryWriter writer, UePropertyTag property)
    {
        WriteFString(writer, property.Name);
        WriteFString(writer, property.TypeName);
        long sizePosition = writer.BaseStream.Position;
        writer.Write(0);
        writer.Write(0);

        long valueStart;
        switch (property.TypeName)
        {
            case "ArrayProperty":
                WriteFString(writer, property.InnerType);
                writer.Write((byte)0);
                valueStart = writer.BaseStream.Position;
                WriteArrayValue(writer, property);
                FillSize(writer, sizePosition, valueStart);
                break;
            case "StructProperty":
                WriteFString(writer, property.StructType);
                writer.Write(new byte[17]);
                valueStart = writer.BaseStream.Position;
                WriteStructValue(writer, property);
                FillSize(writer, sizePosition, valueStart);
                break;
            case "EnumProperty":
                WriteFString(writer, property.EnumType);
                writer.Write((byte)0);
                valueStart = writer.BaseStream.Position;
                WriteFString(writer, property.Value as string);
                FillSize(writer, sizePosition, valueStart);
                break;
            case "BoolProperty":
                writer.Write((byte)((property.Value as bool?) == true ? 1 : 0));
                writer.Write((byte)0);
                FillSize(writer, sizePosition, writer.BaseStream.Position);
                break;
            default:
                writer.Write((byte)0);
                valueStart = writer.BaseStream.Position;
                WriteSimpleValue(writer, property);
                FillSize(writer, sizePosition, valueStart);
                break;
        }
    }

    private static void WriteArrayValue(BinaryWriter writer, UePropertyTag property)
    {
        if (string.Equals(property.InnerType, "StructProperty", StringComparison.OrdinalIgnoreCase))
        {
            writer.Write(property.Nested.Count);
            string elementName = property.ElementName ?? property.Name;
            string structType = property.StructType ?? "";

            using MemoryStream elementStream = new();
            using (BinaryWriter elementWriter = new(elementStream, Encoding.UTF8, leaveOpen: true))
            {
                foreach (UePropertyTag element in property.Nested)
                {
                    WriteProperties(elementWriter, element.Nested);
                }
            }

            WriteFString(writer, elementName);
            WriteFString(writer, "StructProperty");
            writer.Write((int)elementStream.Length);
            writer.Write(0);
            WriteFString(writer, structType);
            writer.Write(new byte[16]);
            writer.Write((byte)0);
            writer.Write(elementStream.ToArray());
            return;
        }

        if (property.Value is byte[] bytes)
        {
            writer.Write(bytes.Length);
            writer.Write(bytes);
            return;
        }

        writer.Write(0);
    }

    private static void WriteStructValue(BinaryWriter writer, UePropertyTag property)
    {
        if (property.Value is byte[] rawBytes && rawBytes.Length > 0)
        {
            writer.Write(rawBytes);
            return;
        }

        WriteProperties(writer, property.Nested);
    }

    private static void WriteSimpleValue(BinaryWriter writer, UePropertyTag property)
    {
        switch (property.TypeName)
        {
            case "IntProperty":
                writer.Write(Convert.ToInt32(property.Value ?? 0));
                break;
            case "UInt32Property":
                writer.Write(Convert.ToUInt32(property.Value ?? 0));
                break;
            case "Int64Property":
                writer.Write(Convert.ToInt64(property.Value ?? 0L));
                break;
            case "FloatProperty":
                writer.Write(Convert.ToSingle(property.Value ?? 0f));
                break;
            case "DoubleProperty":
                writer.Write(Convert.ToDouble(property.Value ?? 0d));
                break;
            case "StrProperty":
            case "NameProperty":
                WriteFString(writer, property.Value as string ?? "");
                break;
            default:
                if (property.Value is byte[] bytes)
                {
                    writer.Write(bytes);
                }
                break;
        }
    }

    private static void FillSize(BinaryWriter writer, long sizePosition, long valueStart)
    {
        long endPosition = writer.BaseStream.Position;
        writer.BaseStream.Position = sizePosition;
        writer.Write((int)(endPosition - valueStart));
        writer.BaseStream.Position = endPosition;
    }

    private static string? ReadFString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length == 0)
        {
            return null;
        }

        if (length < 0)
        {
            byte[] bytes = reader.ReadBytes(-length * 2);
            return Encoding.Unicode.GetString(bytes, 0, Math.Max(0, bytes.Length - 2));
        }

        byte[] asciiBytes = reader.ReadBytes(length);
        return Encoding.ASCII.GetString(asciiBytes, 0, Math.Max(0, asciiBytes.Length - 1));
    }

    private static void WriteFString(BinaryWriter writer, string? value)
    {
        if (value is null)
        {
            writer.Write(0);
            return;
        }

        byte[] bytes = Encoding.ASCII.GetBytes(value);
        writer.Write(bytes.Length + 1);
        writer.Write(bytes);
        writer.Write((byte)0);
    }
}
