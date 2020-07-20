using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using ChatUIXForms.Helpers;
using ChatUIXForms.Models;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace ChatUIXForms.ViewModels
{
    public class ChatPageViewModel: INotifyPropertyChanged
    {
        private Random random = new Random();
        public bool ShowScrollTap { get; set; } = false;
        public bool LastMessageVisible { get; set; } = true;
        public int PendingMessageCount { get; set; } = 0;
        public bool PendingMessageCountVisible { get { return PendingMessageCount > 0; } }
        public Queue<Message> DelayedMessages { get; set; } = new Queue<Message>();
        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();
        public string TextToSend { get; set; }
        public ICommand OnSendCommand { get; set; }
        public ICommand MessageAppearingCommand { get; set; }
        public ICommand MessageDisappearingCommand { get; set; }

        public ChatPageViewModel()
        {
            MessageAppearingCommand = new Command<Message>(OnMessageAppearing);
            MessageDisappearingCommand = new Command<Message>(OnMessageDisappearing);

            OnSendCommand = new Command(() =>
            {
                PendingMessageCount = 0;

                if (!string.IsNullOrEmpty(TextToSend))
                {
                    Messages.Insert(0, new Message() { Text = TextToSend, User = App.User });
                    TextToSend = string.Empty;
                }

            });
        }

        void OnMessageAppearing(Message message)
        {
            string responseIntention = String.Empty;
            var resultIntention = String.Empty;
            var idx = Messages.IndexOf(message);
            if (idx <= 6)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (PendingMessageCount == 0)
                    {
                        resultIntention = GetIntentionsByText(message.Text).GetAwaiter().GetResult();

                        switch (resultIntention)
                        {
                            case IntentsEnumerable.SALUDO:
                                responseIntention = GetQuestion(new List<string>{ "Hola! Espero que te encuentres muy bien! por mi parte estoy muy bien solo que preocupada por el avance de la pandemia.",
                                                                                   "Hola! Mi nombre es Bot y estoy para brindarte toda la información posible sobre la pandemia que nos aqueja.",
                                                                                   "Hola! Estoy enfocada en ayudar a la comunidad con las preguntas que tengan sobre Coronavirus." });
                                break;

                            case IntentsEnumerable.INSULTO:
                                responseIntention = GetQuestion(new List<string>{ "No me gusta que me traten asi, me pone triste =(. Mejor hablemos de algo relacionado al coronavirus, por favor elegi una de las opciones.",
                                                                                   "La verdad es que no me interesa seguir hablando en estos términos. Por favor, volvamos a lo nuestro.",
                                                                                   "No me gusta que me hablen asi. Podemos charlar de otros temas." }); 
                                break;

                            case IntentsEnumerable.EMERGENCIA:
                                responseIntention = "Según la OMS, los síntomas más comunes son: Fiebre, cansancio y tos seca. Por otro lado, otros síntomas que pueden presentar son: Presentar dolores, congestión nasal, rinorrea, dolor de garganta o diarrea."; 
                            break;

                            case IntentsEnumerable.DESPEDIDA:
                                responseIntention = GetQuestion(new List<string>{ "Fue un gusto responder tus consultas. Estoy para ayudarte. Si querés preguntar algo más, simplemente escribime.",
                                                                                   "Hasta luego! Cualquier otra consulta que tengas, estaré aquí para responder.",
                                                                                   "Nos vemos!, estoy a disposición cualquier preguntas que quieras hacer!." }); 
                                break;

                            case IntentsEnumerable.SINRESPUESTA:
                                responseIntention = "No encontré nada en mi superinteligente. Haceme otra pregunta";
                                break;
                        }
                        Messages.Insert(0, new Message() { Text = responseIntention });
                    }
                    ShowScrollTap = false;
                    LastMessageVisible = true;
                    PendingMessageCount = 1;
                });
            }
        }

        void OnMessageDisappearing(Message message)
        {
            var idx = Messages.IndexOf(message);
            if (idx >= 6)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ShowScrollTap = true;
                    LastMessageVisible = false;
                });

            }
        }
        public async Task<string> GetIntentionsByText(string text)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "SUSCRIPTION KEY LUIS");
            queryString["q"] = text;
            queryString["timezoneOffset"] = "-360";
            queryString["verbose"] = "true";
            queryString["spellCheck"] = "false";
            queryString["staging"] = "false";

            var endpointUri = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/APPIDKEY?" + queryString;
            var response = await client.GetAsync(endpointUri).ConfigureAwait(false);

            var strResponseContent = await response.Content.ReadAsStringAsync();
            var luisResult = JsonConvert.DeserializeObject<IntentsResult>(strResponseContent);

            if (luisResult.topScoringIntent.Score > 0.6)
                return luisResult.topScoringIntent.Intent;
            else
                return "None";

        }

        public string GetQuestion(List<string> questions)
        {
            int index = random.Next(questions.Count);
            return questions[index];
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
