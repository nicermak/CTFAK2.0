﻿using System;
using System.IO;
using CTFAK.Memory;
using CTFAK.MMFParser.CCN;
using CTFAK.Utils;

namespace CTFAK.MMFParser.Shared.Events;

public class Expression : ChunkLoader
{
    private int _unk;

    public ChunkLoader Loader;
    public int Num;
    public int ObjectInfo;
    public int ObjectInfoList;
    public int ObjectType;
    public int Unk1;
    public ushort Unk2;

    public override void Write(ByteWriter Writer)
    {
        Writer.WriteInt16((short)ObjectType);
        Writer.WriteInt16((short)Num);
        if (ObjectType == 0 && Num == 0) return;
        var newWriter = new ByteWriter(new MemoryStream());
        if (ObjectType == (int)Constants.ObjectType.System &&
            (Num == 0 || Num == 3 || Num == 23 || Num == 24 || Num == 50))
        {
            if (Loader == null) throw new NotImplementedException("Broken expression: " + Num);
            Loader.Write(newWriter);
        }
        else if (ObjectType >= 2 || ObjectType == -7)
        {
            newWriter.WriteInt16((short)ObjectInfo);
            newWriter.WriteInt16((short)ObjectInfoList);
            if (Num == 16 || Num == 19) Loader.Write(newWriter);
        }

        Writer.WriteInt16((short)(newWriter.Size() + 6));
        Writer.WriteWriter(newWriter);
    }

    public override void Read(ByteReader reader)
    {
        var currentPosition = reader.Tell();
        ObjectType = Settings.Old ? reader.ReadSByte() : reader.ReadInt16();
        Num = Settings.Old ? reader.ReadSByte() : reader.ReadInt16();

        if (ObjectType == 0 && Num == 0) return;

        var size = reader.ReadInt16();
        if (ObjectType == (int)Constants.ObjectType.System)
        {
            if (Num == 0)
            {
                Loader = new LongExp();
            }
            else if (Num == 3)
            {
                Loader = new StringExp();
            }
            else if (Num == 23)
            {
                Loader = new DoubleExp();
            }
            else if (Num == 24)
            {
                Loader = new GlobalCommon();
            }
            else if (Num == 50)
            {
                Loader = new GlobalCommon();
            }
            else if (ObjectType >= 2 || ObjectType == -7)
            {
                ObjectInfo = reader.ReadUInt16();
                ObjectInfoList = reader.ReadInt16();
                if (Num == 16 || Num == 19)
                    Loader = new ExtensionExp();
                else
                    _unk = reader.ReadInt32();
            }
        }
        else if (ObjectType >= 2 || ObjectType == -7)
        {
            ObjectInfo = reader.ReadUInt16();
            ObjectInfoList = reader.ReadInt16();
            if (Num == 16 || Num == 19) Loader = new ExtensionExp();
        }

        Loader?.Read(reader);
        // Unk1 = reader.ReadInt32();
        // Unk2 = reader.ReadUInt16();
        reader.Seek(currentPosition + size);
    }

    public override string ToString()
    {
        return $"Expression {ObjectType}=={Num}: {((ExpressionLoader)Loader)?.Value}";
    }
}

public class ExpressionLoader : ChunkLoader
{
    public object Value;

    public override void Read(ByteReader reader)
    {
        throw new NotImplementedException();
    }

    public override void Write(ByteWriter Writer)
    {
        throw new NotImplementedException();
    }
}

public class StringExp : ExpressionLoader
{
    public override void Read(ByteReader reader)
    {
        Value = reader.ReadUniversal();
    }

    public override void Write(ByteWriter Writer)
    {
        Writer.WriteUnicode((string)Value, true);
    }
}

public class LongExp : ExpressionLoader
{
    public int Val1;

    public override void Read(ByteReader reader)
    {
        Value = reader.ReadInt32();
    }

    public override void Write(ByteWriter Writer)
    {
        Writer.WriteInt32((int)Value);
    }
}

public class ExtensionExp : ExpressionLoader
{
    public override void Read(ByteReader reader)
    {
        Value = reader.ReadInt16();
    }

    public override void Write(ByteWriter Writer)
    {
        Writer.WriteInt16((short)Value);
    }
}

public class DoubleExp : ExpressionLoader
{
    public float FloatValue;

    public override void Read(ByteReader reader)
    {
        Value = reader.ReadDouble();
        FloatValue = reader.ReadSingle();
    }

    public override void Write(ByteWriter Writer)
    {
        Writer.WriteDouble((double)Value);
        Writer.WriteSingle(FloatValue);
    }
}

public class GlobalCommon : ExpressionLoader
{
    public override void Read(ByteReader reader)
    {
        reader.ReadInt32();
        Value = reader.ReadInt32();
    }

    public override void Write(ByteWriter Writer)
    {
        Writer.WriteInt32(0);
        Writer.WriteInt32((int)Value);
    }
}