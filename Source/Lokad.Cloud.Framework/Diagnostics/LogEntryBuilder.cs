#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace Lokad.Cloud.Diagnostics
{
    public sealed class LogEntryBuilder
    {
        private readonly ILog _log;
        private readonly LogLevel _level;
        private readonly List<XElement> _meta;
        private Exception _exception;

        internal LogEntryBuilder(ILog log, LogLevel level)
        {
            _log = log;
            _level = level;
            _meta = new List<XElement>();
        }

        public LogEntryBuilder WithException(Exception exception)
        {
            _exception = exception;
            return this;
        }

        public LogEntryBuilder WithMeta(XElement element)
        {
            _meta.Add(element);
            return this;
        }

        public LogEntryBuilder WithMeta(params XElement[] elements)
        {
            _meta.AddRange(elements);
            return this;
        }

        public LogEntryBuilder WithMeta(string key, string value)
        {
            _meta.Add(new XElement(key, value));
            return this;
        }

        public void Write(object message)
        {
            _log.Log(_level, _exception, message, _meta.ToArray());
        }

        public void WriteFormat(string format, params object[] args)
        {
            _log.Log(_level, _exception, string.Format(CultureInfo.InvariantCulture, format, args), _meta.ToArray());
        }
    }
}
