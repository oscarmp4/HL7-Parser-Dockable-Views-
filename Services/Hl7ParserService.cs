using System;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Base.Util;

namespace HL7ParserWin_ParseView.Services
{
    public class Hl7ParserService
    {
        private readonly PipeParser _parser = new PipeParser();

        public IMessage Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("HL7 message is empty.");

            text = text.Replace("\r\n", "\r").Replace("\n", "\r");
            return _parser.Parse(text);
        }

        public string GetField(IMessage message, string path)
        {
            try
            {
                var terser = new Terser(message);
                return terser.Get(path) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public string ToXml(IMessage msg)
        {
            var xmlParser = new DefaultXMLParser();
            return xmlParser.Encode(msg);
        }

        public string ToPipe(IMessage msg) => _parser.Encode(msg);
    }
}
