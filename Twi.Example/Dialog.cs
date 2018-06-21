using System;
using System.Windows.Forms;

namespace Twi.Example
{
	public partial class Dialog : Form
	{
		public string PinCode { get; private set; }

		public Dialog()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			PinCode = textBox1.Text;
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
