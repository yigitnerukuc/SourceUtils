﻿using System;
using System.IO;
using MimeTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ziks.WebServer;

namespace SourceUtils.WebExport
{
    [JsonConverter( typeof(UrlConverter) )]
    public struct Url : IEquatable<Url>
    {
        public static implicit operator Url( string value )
        {
            return new Url( value );
        }

        public static implicit operator string( Url url )
        {
            return url.Value;
        }

        public readonly string Value;

        public Url( string value )
        {
            Value = value;
        }

        public bool Equals( Url other )
        {
            return string.Equals( Value, other.Value );
        }

        public override bool Equals( object obj )
        {
            if ( ReferenceEquals( null, obj ) ) return false;
            return obj is Url && Equals( (Url) obj );
        }

        public override int GetHashCode()
        {
            return Value != null ? Value.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public class UrlConverter : JsonConverter
    {
        private static readonly DateTime _sExportTime = DateTime.UtcNow;

        private static string GetExportVersionHash()
        {
            return GetFileVersionHash(_sExportTime);
        }

        private static string GetFileVersionHash(DateTime timestamp)
        {
            var major = (int)(timestamp - new DateTime(2000, 1, 1)).TotalDays;
            var minor = (int)(timestamp - new DateTime(timestamp.Year, timestamp.Month, timestamp.Day)).TotalSeconds;
            return $"{major:x}-{minor:x}";
        }

        private static bool ShouldAppendVersionSuffix( Url url )
        {
            switch ( Path.GetExtension( url ).ToLower() )
            {
                case ".js":
                case ".css":
                    return true;
                default:
                    return false;
            }
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            var url = (Url) value;
            if ( Program.IsExporting )
            {
                Program.AddExportUrl( url );

                var suffix = ShouldAppendVersionSuffix( url ) ? $"?v={GetExportVersionHash()}" : "";
                writer.WriteValue( $"{Program.ExportOptions.UrlPrefix}{url.Value}{suffix}" );
            }
            else writer.WriteValue(url.Value);
        }

        public override object ReadJson( JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer )
        {
            return new Url( reader.ReadAsString() );
        }

        public override bool CanConvert( Type objectType )
        {
            return objectType == typeof(Url);
        }
    }

    class ResourceController : Controller
    {
        private static readonly JsonSerializer _sSerializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        protected override void OnServiceText( string text )
        {
            var ext = Path.GetExtension( Request.Url.AbsolutePath );

            Response.ContentType = MimeTypeMap.GetMimeType( ext );

            using ( var writer = new StreamWriter( Response.OutputStream ) )
            {
                writer.Write( text );
            }
        }

        [ResponseWriter]
        public void OnWriteObject( object obj )
        {
            OnServiceJson( obj == null ? null : JObject.FromObject( obj, _sSerializer ) );
        }

        protected bool Skip => Request.QueryString["skip"] == "1";

        protected override void OnServiceJson( JToken token )
        {
            Response.ContentType = MimeTypeMap.GetMimeType( ".json" );

            if ( token != null )
            {
                using ( var writer = new StreamWriter( Response.OutputStream ) )
                {
                    writer.Write( token.ToString( Formatting.None ) );
                }
            }
        }
    }
}