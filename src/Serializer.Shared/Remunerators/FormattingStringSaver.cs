﻿using System;

namespace Das.Serializer.Remunerators;

public class FormattingStringSaver : StringSaver
{
   public FormattingStringSaver(ISerializerSettings settings)
   {
      _indenter = settings.Indentation;
      _newLine = settings.NewLine;
   }

   public sealed override void PrintCurrentTabs()
   {
      AppendRepeatedly('\t', _tabCount);
   }

   public sealed override void TabIn()
   {
      _tabCount--;
   }

   public sealed override void TabOut()
   {
      _tabCount++;
   }

   public sealed override void NewLine()
   {
      Append(_newLine);
      PrintCurrentTabs();
   }

   public sealed override void IndentRepeatedly(Int32 count)
   {
      for (var c = 0; c < count; c++)
         Append(_indenter);
   }

   protected Int32 _tabCount;
   protected readonly String _indenter;
   //private readonly Int32 _indentLength;
   protected readonly String _newLine;

}