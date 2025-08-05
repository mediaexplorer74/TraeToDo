using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraeToDo.Models;
using Windows.UI.Xaml.Data;

namespace TraeToDo.Converters
{
    public class ChatMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var chatMessage = value as ChatMessage;
            return chatMessage.Content;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
