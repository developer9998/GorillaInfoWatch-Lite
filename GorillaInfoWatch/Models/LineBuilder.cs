using GorillaInfoWatch.Models.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GorillaInfoWatch.Models
{
    public class LineBuilder(List<InfoLine> lines) : InfoContent
    {
        public override int SectionCount => Mathf.CeilToInt(Lines.Count / (float)Constants.SectionCapacity);

        public List<InfoLine> Lines = lines ?? [];

        private readonly StringBuilder str = new();

        public LineBuilder() : this([])
        {

        }

        public LineBuilder(string content) : this([.. content.Split(Environment.NewLine).Select(line => new InfoLine(line))])
        {

        }

        public override string GetTitleOfSection(int section) => string.Empty;

        public override IEnumerable<InfoLine> GetLinesAtSection(int section) => Lines.Skip(section * Constants.SectionCapacity).Take(Constants.SectionCapacity);

        public LineBuilder Append(object value)
        {
            str.Append(value);
            return this;
        }

        public LineBuilder AppendLine() => Add();
        public LineBuilder AppendLine(object value) => Append(value).AppendLine();

        public LineBuilder AppendColour(object value, Color colour) => BeginColour(colour).Append(value).EndColour();
        public LineBuilder AppendColour(object value, string hex) => BeginColour(hex).Append(value).EndColour();
        public LineBuilder BeginColour(Color colour) => BeginColour(ColorUtility.ToHtmlStringRGB(colour));
        public LineBuilder BeginColour(string hex) => Append($"<color=#{hex.TrimStart('#')}>");
        public LineBuilder EndColour() => Append("</color>");

        public LineBuilder BeginCentre() => BeginAlign("center");
        public LineBuilder BeginAlign(string alignment) => Append($"<align=\"{alignment}\">");
        public LineBuilder EndAlign() => Append("</align>");

        public LineBuilder Skip()
        {
            Add(string.Empty);
            return this;
        }

        public LineBuilder Add(params List<Widget_Base> widgets)
        {
            if (str.Length > 0)
            {
                Add(str.ToString(), widgets);
                str.Clear();
            }
            return this;
        }

        public LineBuilder Add(string text, params List<Widget_Base> widgets)
        {
            Lines.Add(new(text, widgets));
            return this;
        }

        public LineBuilder AddRange(string[] array, params List<Widget_Base> widgets)
        {
            for (int i = 0; i < array.Length; i++)
            {
                Add(array[i], i == 0 ? widgets : null);
            }
            return this;
        }

        public LineBuilder Insert(int position, string text, params List<Widget_Base> widgets)
        {
            Lines.Insert(position, new(text, widgets));
            return this;
        }

        public LineBuilder Repeat(int amount, string text, params List<Widget_Base> widgets)
        {
            Lines.AddRange(Enumerable.Repeat<InfoLine>(new(text, widgets), amount));
            return this;
        }

        public LineBuilder Clear()
        {
            Lines.Clear();
            return this;
        }

        public static implicit operator List<InfoLine>(LineBuilder lines) => lines.Lines;
    }
}