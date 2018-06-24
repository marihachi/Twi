using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;

namespace Twi.Example
{
	public partial class Form1 : Form
	{
		private TwiClient Client { get; set; }
		private string FilePath { get; set; }

		private const string ConsumerKey = "Please_set_ConsumerKey_here";
		private const string ConsumerSecret = "Please_set_ConsumerSecret_here";

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			Client = new TwiClient(new HttpClient(), ConsumerKey, ConsumerSecret);

			panel1.Enabled = false;
		}

		private async void button1_Click(object sender, EventArgs e)
		{
			try
			{
				var authUrl = await Client.GetAuthorizationUrl();
				Process.Start(authUrl.ToString());

				var dialog = new Dialog();
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					await Client.Authorize(dialog.PinCode);
					panel1.Enabled = true;
					MessageBox.Show("連携が完了しました", "Twi.Example", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
			catch (TwitterException ex)
			{
				MessageBox.Show(ex.Message, "Twi.Example", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private async void button2_Click(object sender, EventArgs e)
		{
			try
			{
				long? mediaId = null;
				if (FilePath != null)
				{
					var fileName = Path.GetFileName(FilePath);

					byte[] buf;
					using (var f = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
					{
						buf = new byte[f.Length];
						await f.ReadAsync(buf, 0, buf.Length);
					}

					mediaId = await Client.UploadMedia(buf, fileName);
				}

				var parameters = new Dictionary<string, string> { { "status", textBox1.Text } };
				if (mediaId != null)
					parameters.Add("media_ids", mediaId.Value.ToString());

				var res = await Client.Request(
					HttpMethod.Post,
					"https://api.twitter.com/1.1/statuses/update.json",
					parameters);

				MessageBox.Show("投稿しました", "Twi.Example", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (TwitterException ex)
			{
				MessageBox.Show($"投稿に失敗しました\r\n{ex.Message}\r\n{ex.StackTrace}", "Twi.Example", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			FilePath = null;
			label1.Text = "メディアが選択されていません";

			var dialog = new OpenFileDialog();
			dialog.Filter = "画像ファイル|*.png;*.jpg;*.jpeg";
			if (dialog.ShowDialog() == DialogResult.OK)
			{
				FilePath = dialog.FileName;
				label1.Text = Path.GetFileName(FilePath);
			}
		}
	}
}
