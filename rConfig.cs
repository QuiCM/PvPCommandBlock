using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;

namespace PvPCommandBlock
{
    public class regionObj
    {
        public string Name;
        public bool BlockCmds;

        public regionObj(string na, bool block)
        {
            Name = na;
            BlockCmds = block;
        }
    }

    public class RegionSet
    {
        public List<regionObj> regionList;

        public RegionSet(List<regionObj> regionList)
        {
            this.regionList = regionList;
        }
    }

    public class rConfig
    {
        public List<RegionSet> RegionConfiguration;

        public static rConfig Read(string path)
        {
            if (!File.Exists(path))
                return new rConfig();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static rConfig Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<rConfig>(sr.ReadToEnd());
                if (ConfigRead != null)
                    ConfigRead(cf);
                return cf;
            }
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public void Write(Stream stream)
        {
            RegionConfiguration = new List<RegionSet>();
            RegionConfiguration.Add(new RegionSet(Maincs.regions));

            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }

        public static Action<rConfig> ConfigRead;
    }
}
