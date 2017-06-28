using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using OWTracker.Data;
using OWTracker.Properties;

namespace OWTracker
{
    public partial class ImportGames : Form
    {
        public ImportGames()
        {
            InitializeComponent();
        }

        private void ImportGames_Load(object sender, EventArgs e)
        {
            MessageBox.Show(Resources.ImportWarning);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            int rowNum = 0;
            try
            {
                var games = new List<Game>();

                //Validate Games
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row == dataGridView1.Rows[dataGridView1.Rows.Count - 1]) break;

                    var g = new Game
                    {
                        Date = DateTimeOffset.Parse(row.Cells["Date"].Value.ToString()),
                        SkillRating = short.Parse(row.Cells["Rank"].Value.ToString()),
                        GameWon = row.Cells["ChangePerGame"].Value?.ToString().ToLower() == "draw" ? null : int.Parse(row.Cells["ChangePerGame"].ToString()) > 0 ? true : int.Parse(row.Cells["ChangePerGame"].ToString()) < 0 ? (bool?)false : null,
                        Map = row.Cells["Map"]?.Value?.ToString(),
                        AttackFirst = row.Cells["StartingSide"].Value == null ? null : row.Cells["StartingSide"].Value?.ToString().Trim().ToLower() == "attack" || row.Cells["StartingSide"].Value?.ToString().Trim() == "1" ? true : row.Cells["StartingSide"].Value?.ToString().Trim().ToLower() == "defend" || row.Cells["StartingSide"].Value?.ToString().Trim() == "0" ? false : (bool?)null,
                        Rounds = row.Cells["Rounds"].Value == null ? null : (byte?)byte.Parse(row.Cells["Rounds"].Value.ToString()),
                        Score = row.Cells["Score"].Value == null ? null : Regex.IsMatch(row.Cells["Score"].Value?.ToString(), @"^\d{1,2}-\d{1,2}$") ? row.Cells["Score"].Value?.ToString() : throw new FormatException("Score was not in the correct format"),
                        Heroes = row.Cells["Heroes"]?.Value?.ToString(),
                        Notes = row.Cells["Notes"]?.Value?.ToString()
                    };
                    games.Add(g);
                    rowNum++;
                }

                //Prompt
                if (MessageBox.Show(string.Format(Resources.ImportPrompt, games.Count, numericUpDown1.Value), "Import Games", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

                rowNum = 0;
                //Add
                foreach (Game game in games)
                {
                    await Config.LoggedInUser.AddGame(new Game(Config.LoggedInUser.UserId, (short)numericUpDown1.Value, game.Date, game.SkillRating, game.GameWon, game.Heroes, game.Map, game.AttackFirst, game.Rounds, game.Score, null, null, game.Notes));
                    rowNum++;
                }

                MessageBox.Show(Resources.ImportFinished);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Resources.ImportError, rowNum, ex.Message));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //shamelessly stolen from the internet

            try
            {
                char[] rowSplitter = { '\n', '\r' };
                const char columnSplitter = '\t';

                IDataObject dataInClipboard = Clipboard.GetDataObject();

                string stringInClipboard =
                    dataInClipboard.GetData(DataFormats.Text).ToString();

                string[] rowsInClipboard = stringInClipboard.Split(rowSplitter, StringSplitOptions.RemoveEmptyEntries);

                int r = dataGridView1.SelectedCells[0].RowIndex;
                int c = dataGridView1.SelectedCells[0].ColumnIndex;

                if (dataGridView1.Rows.Count < r + rowsInClipboard.Length)
                    dataGridView1.Rows.Add(r + rowsInClipboard.Length - dataGridView1.Rows.Count);

                int iRow = 0;
                while (iRow < rowsInClipboard.Length)
                {
                    string[] valuesInRow = rowsInClipboard[iRow].Split(columnSplitter);

                    int jCol = 0;
                    while (jCol < valuesInRow.Length)
                    {
                        if (dataGridView1.ColumnCount - 1 >= c + jCol)
                        {
                            dataGridView1.Rows[r + iRow].Cells[c + jCol].Value =
                                valuesInRow[jCol];
                        }

                        jCol += 1;
                    }

                    iRow += 1;
                }
            }
            catch { }
        }
    }
}