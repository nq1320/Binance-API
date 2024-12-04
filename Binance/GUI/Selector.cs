using System;
using System.Collections.Generic;
using System.Windows.Forms;
using UAPI;

namespace Binance
{
	public partial class Selector : UapiInstrumentSelector
	{
		InstrumentIdentifier SelectedInId;

		public Selector()
		{
			InitializeComponent();
		}

		protected override void Opened(SelectionKind SelectionMode, TimeFrame DefaultTimeFrame)
		{
			// check if the DefaultTimeFrame is supported by the feed - output a message if not

			switch (SelectionMode)
			{
				case SelectionKind.Instruments:
				case SelectionKind.TimeSeries:

					break;
				case SelectionKind.ForwardCurve:

					break;
				case SelectionKind.Trading:

					break;
			}
		}

		private void tmSearch_Tick(object sender, EventArgs e)
		{
			tmSearch.Stop();

			Binance.SymbolSearchRequest SymbolSearch = new Binance.SymbolSearchRequest();
			SymbolSearch.Keywords = tbSearch.Text;
			Binance.I.Request(SymbolSearch);
			if (!SymbolSearch.Wait())
				return;

			SelectedInId = null;
			lvResults.Items.Clear();

			foreach (var Result in SymbolSearch.Results)
			{
				var Item = new ListViewItem(new string[] { Result.InId.CodeName, Result.Info.LongName, Result.Info.AssetType, "Any Extra Fields" });
				Item.Tag = Result.InId;
				lvResults.Items.Add(Item);
			}

			foreach (ColumnHeader ch in lvResults.Columns)
				ch.Width = -1;

			lvResults.Update();
		}

		private void tbSearch_TextChanged(object sender, EventArgs e)
		{
			tmSearch.Stop();
			SelectedInId = null;
			if (!string.IsNullOrWhiteSpace(tbSearch.Text) && tbSearch.TextLength > 1)
				tmSearch.Start();
		}

		private void lvResults_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (lvResults.SelectedItems.Count > 0)
			{
				SelectedInId = (InstrumentIdentifier)lvResults.SelectedItems[0].Tag;
				ReturnSelection();
			}
			else
			{
				SelectedInId = null;
			}
		}

		private void lvResults_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				if (lvResults.SelectedItems.Count > 0)
					SelectedInId = (InstrumentIdentifier)lvResults.SelectedItems[0].SubItems[0].Tag;

				Selector_ReturnKey(ref e);
			}
		}

		void Selector_ReturnKey(ref KeyEventArgs e)
		{
			e.SuppressKeyPress = true;
			e.Handled = true;
			ReturnSelection();
		}

		void ReturnSelection()
		{
			if (!(SelectedInId is null))
				SelectInstruments(new InstrumentIdentifier[] { SelectedInId });
		}
	}
}
