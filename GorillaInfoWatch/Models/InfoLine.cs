using GorillaInfoWatch.Models.Widgets;
using System.Collections.Generic;

namespace GorillaInfoWatch.Models
{
    public class InfoLine(string text, params List<Widget_Base> widgets)
    {
        public string Text = text;

        public List<Widget_Base> Widgets = widgets is not null ? widgets : [];
    }
}