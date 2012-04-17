#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Globalization;
using System.Xml.Linq;
using Lokad.Cloud.Annotations;

namespace Lokad.Cloud.Diagnostics
{
    public sealed class LogEntryBuilder
    {
        private readonly ILog _log;
        private readonly LogLevel _level;
        private readonly XElement _meta;
        private Exception _exception;

        internal LogEntryBuilder(ILog log, LogLevel level)
        {
            _log = log;
            _level = level;
            _meta = new XElement("Meta");
        }

        public LogEntryBuilder WithException(Exception exception)
        {
            _exception = exception;
            return this;
        }

        public LogEntryBuilder WithMeta(XElement element)
        {
            if (element.Name == "Meta")
            {
                _meta.Add(element.Elements());
            }
            else
            {
                _meta.Add(element);
            }

            return this;
        }

        public LogEntryBuilder WithMeta(params XElement[] elements)
        {
            _meta.Add(elements);
            return this;
        }

        public LogEntryBuilder WithMeta(string key, string value)
        {
            _meta.Add(new XElement(key, value));
            return this;
        }

        public void Write(string message)
        {
            _log.Log(_level, message, _exception, _meta);
        }

        public bool TryWrite(string message)
        {
            try
            {
                _log.Log(_level, message, _exception, _meta);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [StringFormatMethod("format")]
        public void WriteFormat(string format, params object[] args)
        {
            _log.Log(_level, string.Format(CultureInfo.InvariantCulture, format, args), _exception, _meta);
        }

        [StringFormatMethod("format")]
        public bool TryWriteFormat(string format, params object[] args)
        {
            try
            {
                _log.Log(_level, string.Format(CultureInfo.InvariantCulture, format, args), _exception, _meta);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}