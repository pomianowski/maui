using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Provider;

using AndroidUri = Android.Net.Uri;

namespace Microsoft.Maui.Essentials.Implementations
{
	public class SmsImplementation : ISms
	{
		static readonly string smsRecipientSeparator = ";";

		internal static bool IsComposeSupported
			=> Platform.IsIntentSupported(CreateIntent(null, new List<string> { "0000000000" }));

		public Task ComposeAsync(SmsMessage message)
		{
			var intent = CreateIntent(message?.Body, message?.Recipients);

			var flags = ActivityFlags.ClearTop | ActivityFlags.NewTask;
			if (Platform.HasApiLevelN)
				flags |= ActivityFlags.LaunchAdjacent;
			intent.SetFlags(flags);

			Platform.AppContext.StartActivity(intent);

			return Task.FromResult(true);
		}

		static Intent CreateIntent(string body, List<string> recipients)
		{
			Intent intent = null;

			body = body ?? string.Empty;

			if (recipients.All(x => string.IsNullOrWhiteSpace(x)))
			{
				var packageName = Telephony.Sms.GetDefaultSmsPackage(Platform.AppContext);
				if (!string.IsNullOrWhiteSpace(packageName))
				{
					intent = new Intent(Intent.ActionSend);
					intent.SetType(FileSystem.MimeTypes.TextPlain);
					intent.PutExtra(Intent.ExtraText, body);
					intent.SetPackage(packageName);

					return intent;
				}
			}

			// Fall back to normal send
			intent = new Intent(Intent.ActionView);

			if (!string.IsNullOrWhiteSpace(body))
				intent.PutExtra("sms_body", body);

			var recipienturi = string.Join(smsRecipientSeparator, recipients.Select(r => AndroidUri.Encode(r)));

			var uri = AndroidUri.Parse($"smsto:{recipienturi}");
			intent.SetData(uri);

			return intent;
		}
	}
}
