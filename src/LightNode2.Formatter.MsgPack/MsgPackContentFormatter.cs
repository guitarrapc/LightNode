﻿using LightNode2.Core;
using MsgPack.Serialization;
using System;

namespace LightNode2.Formatter
{
    public class MsgPackContentFormatter : ContentFormatterBase
    {
        readonly MsgPack.Serialization.SerializationContext serializationContext;

        public MsgPackContentFormatter(string mediaType = "application/x-msgpack", string ext = "mpk")
            : this(SerializationContext.Default, mediaType, ext)
        {
        }

        public MsgPackContentFormatter(MsgPack.Serialization.SerializationContext serializationContext, string mediaType = "application/x-msgpack", string ext = "mpk")
            : base(mediaType, ext, null)
        {
            this.serializationContext = serializationContext;
        }

        public override void Serialize(System.IO.Stream stream, object obj)
        {
            using (var packer = MsgPack.Packer.Create(stream))
            {
                if (obj == null)
                {
                    packer.PackNull();
                    return;
                }

                var serializer = serializationContext.GetSerializer(obj.GetType());
                serializer.PackTo(packer, obj);
            }
        }

        public override object Deserialize(Type type, System.IO.Stream stream)
        {
            using (var packer = MsgPack.Unpacker.Create(stream))
            {
                if (!packer.Read())
                {
                    //throw SerializationExceptions.NewUnexpectedEndOfStream();
                    throw new MsgPack.InvalidMessagePackStreamException("Stream unexpectedly ends");
                }

                var serializer = serializationContext.GetSerializer(type);
                return serializer.UnpackFrom(packer);
            }
        }
    }

    public class MsgPackContentFormatterFactory : IContentFormatterFactory
    {
        public IContentFormatter CreateFormatter()
        {
            return new MsgPackContentFormatter();
        }
    }
}