using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Windows.Forms;
using Newtonsoft.Json;
using Twi.Exceptions;

namespace Twi.Example
{
	public partial class Form1 : Form
	{

		private TwiClient Client { get; set; }

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
				Process.Start((await Client.GetAuthorizationUrl()).ToString());

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
				var res = await Client.Request(
					HttpMethod.Post,
					"https://api.twitter.com/1.1/statuses/update.json",
					new Dictionary<string, string> { { "status", textBox1.Text } });

				dynamic json = JsonConvert.DeserializeObject(res);
				if (json.errors != null)
				{
					throw new ApplicationException(json.errors[0].message.Value);
				}
				MessageBox.Show("投稿しました", "Twi.Example", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (TwitterException ex)
			{
				MessageBox.Show($"投稿に失敗しました\r\n{ex.Message}\r\n{ex.StackTrace}", "Twi.Example", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
