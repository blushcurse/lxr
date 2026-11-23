using System.Windows;
using Bloxstrap.LXR.Flags;

namespace Bloxstrap.LXR.UI
{
    public partial class AddFlagWindow : LxrWindow
    {
        public string? AddedName { get; private set; }

        public AddFlagWindow() => InitializeComponent();

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string value = ValueBox.Text.Trim();

            string? error = FlagService.ValidateCustomName(name);

            if (error is null && value.Length == 0)
                error = "Enter a value.";

            if (error is null)
            {
                var def = FlagCatalog.Find(name) ?? FlagCatalog.CreateCustom(name);
                error = FlagService.Set(def, value);
            }

            if (error is not null)
            {
                ErrorText.Text = error;
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            AddedName = name;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
