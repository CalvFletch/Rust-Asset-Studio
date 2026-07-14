using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AssetStudio.GUI
{
    using Color = System.Drawing.Color;

    public static class Theme
    {
        public static bool Dark { get; private set; }

        public static readonly Color DarkBack = Color.FromArgb(32, 32, 32);
        public static readonly Color DarkSurface = Color.FromArgb(45, 45, 48);
        public static readonly Color DarkInput = Color.FromArgb(56, 56, 60);
        public static readonly Color DarkText = Color.FromArgb(230, 230, 230);
        public static readonly Color DarkBorder = Color.FromArgb(85, 85, 85);
        public static readonly Color DarkHighlight = Color.FromArgb(0, 122, 204);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static bool IsSystemDark()
        {
            var value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
            return value is int light && light == 0;
        }

        // themeMode: 0 = follow system, 1 = light, 2 = dark
        public static bool ShouldUseDark(int themeMode) => themeMode switch
        {
            1 => false,
            2 => true,
            _ => IsSystemDark(),
        };

        public static void SetMode(bool dark)
        {
            Dark = dark;
            ToolStripManager.Renderer = dark
                ? new ToolStripProfessionalRenderer(new DarkColorTable()) { RoundedEdges = false }
                : new ToolStripProfessionalRenderer();
        }

        public static void Apply(Form form, bool dark)
        {
            SetMode(dark);
            Apply(form);
        }

        public static void Apply(Form form)
        {
            var dark = Dark;
            form.BackColor = dark ? DarkBack : SystemColors.Control;
            form.ForeColor = dark ? DarkText : SystemColors.ControlText;
            ApplyTitleBar(form, dark);
            ApplyControls(form.Controls, dark);
        }

        private static void ApplyTitleBar(Form form, bool dark)
        {
            var value = dark ? 1 : 0;
            if (form.IsHandleCreated)
            {
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
            }
            else
            {
                form.HandleCreated += (s, e) =>
                {
                    var v = Dark ? 1 : 0;
                    DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int));
                };
            }
        }

        private static void ApplyControls(Control.ControlCollection controls, bool dark)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case TreeView tree:
                        tree.BackColor = dark ? DarkSurface : SystemColors.Window;
                        tree.ForeColor = dark ? DarkText : SystemColors.WindowText;
                        tree.LineColor = dark ? DarkBorder : Color.Black;
                        ApplyScrollBars(tree, dark);
                        break;
                    case ListView list:
                        list.BackColor = dark ? DarkSurface : SystemColors.Window;
                        list.ForeColor = dark ? DarkText : SystemColors.WindowText;
                        EnsureListViewHeaderHook(list);
                        list.OwnerDraw = dark;
                        ApplyScrollBars(list, dark);
                        break;
                    case DataGridView grid:
                        grid.EnableHeadersVisualStyles = !dark;
                        grid.BackgroundColor = dark ? DarkBack : SystemColors.AppWorkspace;
                        grid.GridColor = dark ? DarkBorder : SystemColors.ControlDark;
                        grid.DefaultCellStyle.BackColor = dark ? DarkSurface : SystemColors.Window;
                        grid.DefaultCellStyle.ForeColor = dark ? DarkText : SystemColors.WindowText;
                        grid.ColumnHeadersDefaultCellStyle.BackColor = dark ? DarkSurface : SystemColors.Control;
                        grid.ColumnHeadersDefaultCellStyle.ForeColor = dark ? DarkText : SystemColors.ControlText;
                        grid.RowHeadersDefaultCellStyle.BackColor = dark ? DarkSurface : SystemColors.Control;
                        grid.RowHeadersDefaultCellStyle.ForeColor = dark ? DarkText : SystemColors.ControlText;
                        ApplyScrollBars(grid, dark);
                        break;
                    case RichTextBox rich:
                        rich.BackColor = dark ? DarkSurface : SystemColors.Window;
                        rich.ForeColor = dark ? DarkText : SystemColors.WindowText;
                        ApplyScrollBars(rich, dark);
                        break;
                    case TextBox text:
                        text.BackColor = dark ? DarkInput : SystemColors.Window;
                        text.ForeColor = dark ? DarkText : SystemColors.WindowText;
                        break;
                    case ComboBox combo:
                        combo.BackColor = dark ? DarkInput : SystemColors.Window;
                        combo.ForeColor = dark ? DarkText : SystemColors.WindowText;
                        combo.FlatStyle = dark ? FlatStyle.Flat : FlatStyle.Standard;
                        break;
                    case NumericUpDown numeric:
                        numeric.BackColor = dark ? DarkInput : SystemColors.Window;
                        numeric.ForeColor = dark ? DarkText : SystemColors.WindowText;
                        break;
                    case Button button:
                        button.BackColor = dark ? DarkSurface : SystemColors.Control;
                        button.ForeColor = dark ? DarkText : SystemColors.ControlText;
                        button.FlatStyle = dark ? FlatStyle.Flat : FlatStyle.Standard;
                        button.FlatAppearance.BorderColor = DarkBorder;
                        break;
                    case TabControl tab:
                        EnsureTabControlHook(tab);
                        tab.DrawMode = dark ? TabDrawMode.OwnerDrawFixed : TabDrawMode.Normal;
                        foreach (TabPage page in tab.TabPages)
                        {
                            page.BackColor = dark ? DarkBack : SystemColors.Control;
                            page.ForeColor = dark ? DarkText : SystemColors.ControlText;
                        }
                        break;
                    case MenuStrip or StatusStrip or ToolStrip:
                        control.BackColor = dark ? DarkSurface : SystemColors.Control;
                        control.ForeColor = dark ? DarkText : SystemColors.ControlText;
                        if (control is ToolStrip strip)
                        {
                            ApplyToolStripItems(strip.Items, dark);
                        }
                        break;
                    case GroupBox or Label or CheckBox or RadioButton:
                        control.ForeColor = dark ? DarkText : SystemColors.ControlText;
                        break;
                    case Panel or SplitContainer:
                        control.BackColor = dark ? DarkBack : SystemColors.Control;
                        control.ForeColor = dark ? DarkText : SystemColors.ControlText;
                        break;
                }

                if (control.HasChildren)
                {
                    ApplyControls(control.Controls, dark);
                }
            }
        }

        public static void ApplyToolStripItems(ToolStripItemCollection items, bool dark)
        {
            foreach (ToolStripItem item in items)
            {
                item.ForeColor = dark ? DarkText : SystemColors.ControlText;
                if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
                {
                    menuItem.DropDown.BackColor = dark ? DarkSurface : SystemColors.Control;
                    menuItem.DropDown.ForeColor = dark ? DarkText : SystemColors.ControlText;
                    ApplyToolStripItems(menuItem.DropDownItems, dark);
                }
                else if (item is ToolStripComboBox comboItem)
                {
                    comboItem.BackColor = dark ? DarkInput : SystemColors.Window;
                    comboItem.ForeColor = dark ? DarkText : SystemColors.WindowText;
                }
                else if (item is ToolStripTextBox textItem)
                {
                    textItem.BackColor = dark ? DarkInput : SystemColors.Window;
                    textItem.ForeColor = dark ? DarkText : SystemColors.WindowText;
                }
            }
        }

        // "DarkMode_Explorer" gives native dark scrollbars on Windows 10 1809+.
        private static void ApplyScrollBars(Control control, bool dark)
        {
            if (control.IsHandleCreated)
            {
                SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : "Explorer", null);
            }
            else
            {
                control.HandleCreated += (s, e) => SetWindowTheme(control.Handle, Dark ? "DarkMode_Explorer" : "Explorer", null);
            }
        }

        private static readonly HashSet<Control> hookedControls = new HashSet<Control>();

        private static void EnsureTabControlHook(TabControl tab)
        {
            if (!hookedControls.Add(tab))
            {
                return;
            }
            tab.DrawItem += (s, e) =>
            {
                var tc = (TabControl)s;
                var page = tc.TabPages[e.Index];
                var selected = e.Index == tc.SelectedIndex;
                using var back = new SolidBrush(selected ? DarkInput : DarkSurface);
                e.Graphics.FillRectangle(back, e.Bounds);
                TextRenderer.DrawText(e.Graphics, page.Text, tc.Font, e.Bounds, DarkText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private static void EnsureListViewHeaderHook(ListView list)
        {
            if (!hookedControls.Add(list))
            {
                return;
            }
            list.DrawColumnHeader += (s, e) =>
            {
                using var back = new SolidBrush(DarkSurface);
                e.Graphics.FillRectangle(back, e.Bounds);
                using var border = new Pen(DarkBorder);
                e.Graphics.DrawLine(border, e.Bounds.Right - 1, e.Bounds.Top + 2, e.Bounds.Right - 1, e.Bounds.Bottom - 3);
                e.Graphics.DrawLine(border, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                var textBounds = e.Bounds;
                textBounds.Inflate(-4, 0);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, textBounds, DarkText,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            };
            list.DrawItem += (s, e) => e.DrawDefault = true;
            list.DrawSubItem += (s, e) => e.DrawDefault = true;
        }

        private class DarkColorTable : ProfessionalColorTable
        {
            public override Color MenuStripGradientBegin => DarkSurface;
            public override Color MenuStripGradientEnd => DarkSurface;
            public override Color MenuBorder => DarkBorder;
            public override Color MenuItemBorder => DarkHighlight;
            public override Color MenuItemSelected => Color.FromArgb(70, 70, 74);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(70, 70, 74);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(70, 70, 74);
            public override Color MenuItemPressedGradientBegin => DarkInput;
            public override Color MenuItemPressedGradientEnd => DarkInput;
            public override Color ToolStripDropDownBackground => DarkSurface;
            public override Color ImageMarginGradientBegin => DarkSurface;
            public override Color ImageMarginGradientMiddle => DarkSurface;
            public override Color ImageMarginGradientEnd => DarkSurface;
            public override Color SeparatorDark => DarkBorder;
            public override Color SeparatorLight => DarkBorder;
            public override Color StatusStripGradientBegin => DarkSurface;
            public override Color StatusStripGradientEnd => DarkSurface;
            public override Color ToolStripBorder => DarkBorder;
            public override Color ToolStripGradientBegin => DarkSurface;
            public override Color ToolStripGradientMiddle => DarkSurface;
            public override Color ToolStripGradientEnd => DarkSurface;
            public override Color CheckBackground => DarkInput;
            public override Color CheckSelectedBackground => Color.FromArgb(70, 70, 74);
            public override Color CheckPressedBackground => DarkInput;
            public override Color ButtonSelectedHighlight => Color.FromArgb(70, 70, 74);
            public override Color ButtonSelectedGradientBegin => Color.FromArgb(70, 70, 74);
            public override Color ButtonSelectedGradientEnd => Color.FromArgb(70, 70, 74);
        }
    }
}
