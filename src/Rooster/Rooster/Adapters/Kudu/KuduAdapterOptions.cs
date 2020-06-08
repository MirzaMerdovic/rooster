﻿using System;

namespace Rooster.Adapters.Kudu
{
    public class KuduAdapterOptions
    {
        public string User { get; set; }

        public string Password { get; set; }

        public Uri BaseUri { get; set; }
    }
}