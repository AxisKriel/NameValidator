using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using Newtonsoft.Json;

namespace NameValidator
{
	public class Config
	{
		/// <summary>
		/// The action to take against players carrying invalid nicknames.
		/// Valid actions: 'kick', 'ban', 'any message to send to the player'.
		/// </summary>
		public string Action = "kick";

		/// <summary>
		/// The reason to use when the action is set to kick/ban.
		/// </summary>
		public string Reason = "Invalid character name.";

		/// <summary>
		/// A string containing chars a character name can't have.
		/// </summary>
		public string InvalidChars = "";

		/// <summary>
		/// A regex to match character names against. A matching name is deemed as invalid.
		/// </summary>
		public string InvalidNameRegex = "";

		/// <summary>
		/// Whether to kick any player using characters that the Terraria font doesn't natively support.
		/// </summary>
		public bool TerrariaFontOnly = false;

		public static Config Read(string path)
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				if (!File.Exists(path))
				{
					Config config = new Config();
					File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
					return config;
				}
				return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleInfo(ex.Message);
				TShock.Log.Error(ex.ToString());
				return new Config();
			}
		}
	}
}
