using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace NameValidator
{
	[ApiVersion(2, 1)]
	public class NameValidator : TerrariaPlugin
	{
		private FontFamily font;

		public override string Author => "Enerdy";

		public override string Description => "Validate character names on join based on a configuration file.";

		public override string Name => "Name Validator";

		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		private Config Config { get; set; }

		public NameValidator(Main game)
			: base(game)
		{
			Order = 10;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.NetGreetPlayer.Register(this, OnJoin);
			}
		}

		public override void Initialize()
		{
			string fontname = "Andy";
			if ((font = Fonts.SystemFontFamilies.FirstOrDefault(f => f.Source.Equals(fontname, StringComparison.OrdinalIgnoreCase))) == null)
				TShock.Log.ConsoleError($"The font '{fontname}' was not found.");

			string path = Path.Combine(TShock.SavePath, "NameValidator.json");
			Config = Config.Read(path);

			ServerApi.Hooks.NetGreetPlayer.Register(this, OnJoin);

			Commands.ChatCommands.Add(new Command("namevalidator.reload", (args) =>
			{
				Config = Config.Read(path);
				args.Player.SendSuccessMessage("Name Validator config reloaded.");
			}, "nvreload"));
		}

		private void OnJoin(GreetPlayerEventArgs e)
		{
			if (e.Handled || e.Who < 0 || e.Who > Main.player.Length - 1)
				return;

			Player player = Main.player[e.Who];
			// If the player's name is null then it most likely isn't a real player
			if (!String.IsNullOrEmpty(player?.name))
			{
				string name = player.name;
				if (!ValidateString(name))
				{
					switch (Config.Action.ToLowerInvariant())
					{
						case "ban":
							TShock.Utils.Ban(TShock.Players[e.Who], Config.Reason);
							TShock.Log.ConsoleInfo($"Player '{name}' was banned for \"{Config.Reason}\".");
							e.Handled = true;
							return;
						case "kick":
							TShock.Utils.Kick(TShock.Players[e.Who], Config.Reason, silent: true);
							TShock.Log.ConsoleInfo($"Player '{name}' was kicked for \"{Config.Reason}\".");
							e.Handled = true;
							return;
						default:
							TShock.Players[e.Who].SendWarningMessage(Config.Action);
							return;
					}
				}
			}
		}

		/// <summary>
		/// Validates a string according to a configuration file.
		/// </summary>
		/// <param name="s">The string to validate</param>
		/// <returns>Whether the string is valid.</returns>
		private bool ValidateString(string s)
		{
			// Font contains check
			if (Config.TerrariaFontOnly)
			{
				foreach (char c in s)
				{
					ICollection<Typeface> typefaces = font.GetTypefaces();
					foreach (Typeface t in typefaces)
					{
						t.TryGetGlyphTypeface(out GlyphTypeface glyph);
						if (glyph != null && !glyph.CharacterToGlyphMap.ContainsKey(Convert.ToInt16(c)))
						{
							// Spot detected
							return false;
						}
					}
				}
			}

			// Regex check
			if (Config.InvalidNameRegexes != null)
			{
				foreach (string regex in Config.InvalidNameRegexes)
				{
					if (Config.IgnoreCase)
					{
						if (Regex.IsMatch(s, regex, RegexOptions.IgnoreCase))
							return false;
					}
					else
					{
						if (Regex.IsMatch(s, regex))
							return false;
					}
				}
			}

			// Invalid char check and return if valid
			if (Config.IgnoreCase)
				return !Config.InvalidChars.ToLowerInvariant().Intersect(s.ToLowerInvariant()).Any();
			else
				return !Config.InvalidChars.Intersect(s).Any();
		}
	}
}
