﻿using LightNode2.Core;
using ProtoBuf.Meta;
using System;

namespace LightNode2.Formatter
{
    public class ProtoBufContentFormatter : ContentFormatterBase
    {
        readonly RuntimeTypeModel runtimeTypeModel;

        public ProtoBufContentFormatter(string mediaType = "application/x-protobuf", string ext = "proto")
            : this(RuntimeTypeModel.Default, mediaType, ext)
        {
        }
        public ProtoBufContentFormatter(RuntimeTypeModel runtimeTypeModel, string mediaType = "application/x-protobuf", string ext = "proto")
            : base(mediaType, ext, null)
        {
            this.runtimeTypeModel = runtimeTypeModel;
        }

        public override void Serialize(System.IO.Stream stream, object obj)
        {
            runtimeTypeModel.Serialize(stream, obj);
        }

        public override object Deserialize(Type type, System.IO.Stream stream)
        {
            return runtimeTypeModel.Deserialize(stream, null, type);
        }
    }

    public class ProtoBufContentFormatterFactory : IContentFormatterFactory
    {
        public IContentFormatter CreateFormatter()
        {
            return new ProtoBufContentFormatter();
        }
    }
}