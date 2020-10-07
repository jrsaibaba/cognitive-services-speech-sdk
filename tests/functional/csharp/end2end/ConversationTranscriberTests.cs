//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Tests.EndToEnd.Utils;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicrosoftSpeechSDKSamples;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CognitiveServices.Speech.Tests.EndToEnd
{
    using static SPXTEST;
    using static Config;
    using static ConversationTranscriberTestsHelper;
    using static SpeechRecognitionTestsHelper;

    /// <summary>
    /// Class which defines VoiceSignature as specified under https://aka.ms/cts/signaturegenservice.
    /// </summary>
    [DataContract]
    internal class VoiceSignature
    {
        [DataMember]
        public string Status { get; private set; }

        [DataMember]
        public VoiceSignatureData Signature { get; private set; }

        [DataMember]
        public string Transcription { get; private set; }
    }

    /// <summary>
    /// Class which defines VoiceSignatureData which is used when creating/adding participants
    /// </summary>
    [DataContract]
    internal class VoiceSignatureData
    {
        internal VoiceSignatureData()
        { }

        internal VoiceSignatureData(int version, string tag, string data)
        {
            this.Version = version;
            this.Tag = tag;
            this.Data = data;
        }

        [DataMember]
        public int Version { get; private set; }

        [DataMember]
        public string Tag { get; private set; }

        [DataMember]
        public string Data { get; private set; }
    }

    [TestClass]
    public class ConversationTranscriberTests : RecognitionTestBase
    {
        private ConversationTranscriberTestsHelper helper;
        private static string conversationTranscriptionMultiAudioEndpoint;

        // Copy the Signature value from the Response body after calling the RESTFUL API at https://aka.ms/cts/signaturegenservice
        // Voice signature format example: { "Version": <Numeric value>, "Tag": "string", "Data": "string" }
        private const string voiceSignatureKatie = "{ \"Version\": 0, \"Tag\": \"VtZQ7sJp8np3AxQC+87WYyBHWsZohWFBN0zgWzzOnpw=\", \"Data\": \"BhRRgDCrg6ij5fylg5Jpf5ZW/o/uDWi199DYBbfL1Qdspj77qiuvsVKzG2g5Z9jxKtfdwtkKtaDxog9O6pGD7Ot/8mM1jUtt6LKNz4H9EFvznV/dlFk2Oisg8ZKx/RBlNFMYJkQJnxT/zLfhNWiIF5Y97jH4sgRh2orDg6/567FGktAgcESAbiDx1e7tf0TTLdwijw4p1vJ3qJ2cSCdNbXE9KeUd8sClQLDheCPo+et3rMs5W+Rju3W1SJE6ru9gAoH88CyAfI80+ysAecH3GPJYM+j1uhvmWoKIrSfS40BYOe6AUgLNb3a4Pue4oGAmuAyWfwpP1uezleSanpJc73HT91n2UsaHrQj5eK6uuBCjwmu+JI3FT+Vo6JhAARHecdb70U1wyW9vv5t0Q3tV1WNiN/30qSVydDtyrSVpgBiIwlj41Ua22JJCOMrPl7NKLnFmeZ4Hi4aIKoHAxDvrApteL60sxLX/ADAtYCB3Y6iagDyR1IOsIlbaPhL0rQDnC/0z65k7BDekietFNzvvPVoIwJ26GHrXFYYwZe3alVvCsXTpZGBknvSiaCalrixnyGqYo0nG/cd/LodEEIht/PWoFkNlbABqHMbToeI/6j+ICKuVJgTDtcsDQiWKSvrQp9kTSv+cF3LyPVkbks0JvbQhj4AkAv7Rvymgnsu6o8gGyxTX0kxujqCMPCzgFrVd\"}";
        private const string voiceSignatureSteve = "{ \"Version\": 0, \"Tag\": \"HbIvzbfAWjeR/3R+WvUEoeid1AbDaHNOMWItgs7mTxc=\", \"Data\": \"DizY04Z7PH/sYu2Yw2EcL4Mvj1GnEDOWJ/DhXHGdQJsQ8/zDc13z1cwllbEo5OSr3oGoKEHLV95OUA6PgksZzvTkf42iOFEv3yifUNfYkZuIzStZoDxWu1H1BoFBejqzSpCYyvqLwilWOyUeMn+z+E4+zXjqHUCyYJ/xf0C3+58kCbmyA55yj7YZ6OtMVyFmfT2GLiXr4YshUB14dgwl3Y08SRNavnG+/QOs+ixf3UoZ6BC1VZcVQnC2tn2FB+8v6ehnIOTQedo++6RWIB0RYmQ8VaEeI0E4hkpA1OxQ9f2gBVtw3KZXWSWBz8sXig2igpwMsQoFRmmIOGsu+p6tM8/OThQpARZ7OyAxsurzmaSGZAaXYt0YwMdIIXKeDBF6/KnUyw+NNzku1875u2Fde/bxgVvCOwhrLPPuu/RZUeAkwVQge7nKYNW5YjDcz8mfg4LfqWEGOVCcmf2IitQtcIEjY3MwLVNvsAB6GT2es1/1QieCfQKy/Tdu8IUfEvekwSCxSlWhfVrLjRhGeWa9idCjsngQbNkqYUNdnIlidkn2DC4BavSTYXR5lVxV4SR/Vvj8h4N5nP/URPDhkzl7n7Tqd4CGFZDzZzAr7yRo3PeUBX0CmdrKLW3+GIXAdvpFAx592pB0ySCv5qBFhJNErEINawfGcmeWZSORxJg1u+agj51zfTdrHZeugFcMs6Be\"}";

        [ClassInitialize]
        public static void TestClassinitialize(TestContext context)
        {
            LoggingTestBaseInit(context);
            BaseClassInit(context);
            conversationTranscriptionMultiAudioEndpoint = conversationTranscriptionEndpoint + "/multiaudio";
        }

        [ClassCleanup]
        new public static void TestClassCleanup()
        {
            LoggingTestBaseCleanup();
        }

        [TestInitialize]
        public void Initialize()
        {
            helper = new ConversationTranscriberTestsHelper();
        }

        [RetryTestMethod]
        public async Task ConversationIdWithChinese()
        {
            var config = CreateCTSInRoomSpeechConfig();
            var audioInput = AudioConfig.FromWavFileInput(AudioUtterancesMap[AudioUtteranceKeys.CONVERSATION_BETWEEN_TWO_PERSONS_ENGLISH].FilePath.GetRootRelativePath());
            string myConversationId = "的";
            using (var conversation = await Conversation.CreateConversationAsync(config, myConversationId))
            {
                //the UTF8 decoding of 的 is \xe7\x9a\x84, which will be shown in the debugger in the C++ side.
                var gotId = conversation.ConversationId;
                SPXTEST_ARE_EQUAL(myConversationId, gotId);
            }
        }

        [RetryTestMethod]
        public async Task ConversationIdWithAnsiOnly()
        {
            var config = CreateCTSInRoomSpeechConfig();
            string myConversationId = "123 456";
            using (var conversation = await Conversation.CreateConversationAsync(config, myConversationId))
            {
                var gotId = conversation.ConversationId;
                SPXTEST_ARE_EQUAL(myConversationId, gotId);
            }
        }

        [RetryTestMethod]
        public void ConversationCreateUsers()
        {
            string myId = "xyz@example.com";
            var user = User.FromUserId(myId);
            SPXTEST_ARE_EQUAL(myId, user.UserId);
        }

        [RetryTestMethod]
        public void ConversationCreateParticipantWithValidPreferredLanguageAndVoiceSignature()
        {
            bool exception = false;
            try
            {
                var participant = Participant.From("xyz@example.com", "zh-cn", voiceSignatureKatie);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Got Exception: " + ex.Message.ToString());
                exception = true;
            }
            SPXTEST_ARE_EQUAL(exception, false);
        }

        [RetryTestMethod]
        public void ConversationCreateParticipantWithEmptyPreferredLanguage()
        {
            bool exception = false;
            try
            {
                var participant = Participant.From("xyz@example.com", "", voiceSignatureKatie);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Got Exception: " + ex.Message.ToString());
                exception = true;
            }
            SPXTEST_ARE_EQUAL(exception, false);
        }

        [RetryTestMethod]
        public void ConversationCreateParticipantWithInvalidPreferredLanguage()
        {
            bool exception = false;
            try
            {
                var participant = Participant.From("xyz@example.com", "invalid", voiceSignatureKatie);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Got Exception: " + ex.Message.ToString());
                exception = true;
            }
            SPXTEST_ARE_EQUAL(exception, false);
        }

        [RetryTestMethod]
        public void ConversationCreateParticipantWithEmptyVoiceSignature()
        {
            bool exception = false;
            try
            {
                var participant = Participant.From("xyz@example.com", "zh-cn", string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Got Exception: " + ex.Message.ToString());
                exception = true;
            }
            SPXTEST_ARE_EQUAL(exception, false);
        }

        [RetryTestMethod]
        public void ConversationCreateParticipantWithInvalidVoiceSignature()
        {
            bool exception = false;
            try
            {
                var participant2 = Participant.From("xyz@example.com", "", "1.1, 2.2");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Got Exception: " + ex.Message.ToString());
                exception = true;
            }
            SPXTEST_ARE_EQUAL(exception, true);
        }

        [Ignore("Temporarily Disabled see task 2854191")]
        [TestMethod, TestCategory(TestCategory.LongRunning)]
        public async Task ConversationAddParticipant()
        {
            var config = CreateCTSInRoomSpeechConfig();
            var audioInput = AudioConfig.FromWavFileInput(AudioUtterancesMap[AudioUtteranceKeys.CONVERSATION_BETWEEN_TWO_PERSONS_ENGLISH].FilePath.GetRootRelativePath());
            var meetingId = Guid.NewGuid().ToString();
            using (var conversation = await Conversation.CreateConversationAsync(config, meetingId))
            {
                using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
                {
                    await conversationTranscriber.JoinConversationAsync(conversation);

                    await conversation.AddParticipantAsync("OneUserByUserId");

                    var user = User.FromUserId("CreateUserFromId and then add it");
                    await conversation.AddParticipantAsync(user);

                    // Voice signature format as specified here https://aka.ms/cts/signaturegenservice
                    string voice = voiceSignatureKatie;
                    var participant = Participant.From("userIdForParticipant", Language.EN, voice);
                    await conversation.AddParticipantAsync(participant);

                    var result = await helper.GetRecognizerResult(conversationTranscriber, meetingId);
                    SPXTEST_ISTRUE(helper.FindTheRef(result, AudioUtterancesMap[AudioUtteranceKeys.CONVERSATION_BETWEEN_TWO_PERSONS_ENGLISH].Utterances[Language.EN][0].Text));
                }
            }
        }

        [Ignore("Temporarily Disabled see task 2854191")]
        [TestMethod, TestCategory(TestCategory.LongRunning)]
        public async Task ConversationAddParticipantFromSubscription()
        {
            var config = SpeechConfig.FromSubscription(conversationTranscriptionPRODKey, conversationTranscriptionPRODRegion);
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            config.SetProperty(PropertyId.Speech_LogFilename, "carbon.log");

            var audioInput = AudioConfig.FromWavFileInput(AudioUtterancesMap[AudioUtteranceKeys.CONVERSATION_BETWEEN_TWO_PERSONS_ENGLISH].FilePath.GetRootRelativePath());
            var meetingID = Guid.NewGuid().ToString();
            using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
            {
                using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
                {
                    await conversationTranscriber.JoinConversationAsync(conversation);

                    await conversation.AddParticipantAsync("OneUserByUserId");

                    var user = User.FromUserId("CreateUserFromId and then add it");
                    await conversation.AddParticipantAsync(user);

                    // Voice signature format as specified here https://aka.ms/cts/signaturegenservice
                    string voice = voiceSignatureKatie;
                    var participant = Participant.From("userIdForParticipant", Language.EN, voice);
                    await conversation.AddParticipantAsync(participant);

                    var result = await helper.GetRecognizerResult(conversationTranscriber, meetingID);
                    SPXTEST_ISTRUE(helper.FindTheRef(result, AudioUtterancesMap[AudioUtteranceKeys.CONVERSATION_BETWEEN_TWO_PERSONS_ENGLISH].Utterances[Language.EN][0].Text));
                }
            }
        }

        [Ignore("Temporarily Disabled see task 2854191")]
        [TestMethod, TestCategory(TestCategory.LongRunning)]
        public async Task ConversationRemoveParticipant()
        {
            var config = CreateCTSInRoomSpeechConfig();
            var audioInput = AudioConfig.FromWavFileInput(AudioUtterancesMap[AudioUtteranceKeys.CONVERSATION_BETWEEN_TWO_PERSONS_ENGLISH].FilePath.GetRootRelativePath());
            var meetingID = Guid.NewGuid().ToString();
            using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
            {
                conversation.Properties.SetProperty("MTUri", "MTUri_value");
                conversation.Properties.SetProperty("DifferenciateGuestSpeakers", "true");
                conversation.Properties.SetProperty("iCalUId", "iCalUId_value");
                conversation.Properties.SetProperty("callId", "callId_value");
                conversation.Properties.SetProperty("organizer", "organizer_value");
                conversation.Properties.SetProperty("audiorecording", "on");
                conversation.Properties.SetProperty("Threadid", "Threadid_value");
                conversation.Properties.SetProperty("Organizer_Mri", "Organizer_Mri_vaulue");

                using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
                {
                    await conversationTranscriber.JoinConversationAsync(conversation);

                    bool exception = false;
                    try
                    {
                        await conversation.RemoveParticipantAsync("NoneExist");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Got Exception: " + ex.Message.ToString());
                        exception = true;
                    }
                    SPXTEST_ARE_EQUAL(exception, true);

                    await conversation.AddParticipantAsync("OneUserByUserId");
                    await conversation.RemoveParticipantAsync("OneUserByUserId");

                    var user = User.FromUserId("user object created from User.FromUserId");
                    await conversation.AddParticipantAsync(user);
                    await conversation.RemoveParticipantAsync(user);

                    // Voice signature format as specified here https://aka.ms/cts/signaturegenservice
                    var participant = Participant.From("userIdForParticipant", Language.EN, voiceSignatureKatie);
                    await conversation.AddParticipantAsync(participant);
                    await conversation.RemoveParticipantAsync(participant);

                    var result = await helper.GetRecognizerResult(conversationTranscriber, meetingID);
                    SPXTEST_ISTRUE(helper.FindTheRef(result, AudioUtterancesMap[AudioUtteranceKeys.CONVERSATION_BETWEEN_TWO_PERSONS_ENGLISH].Utterances[Language.EN][0].Text));
                }
            }
        }

        [TestMethod, TestCategory(TestCategory.LongRunning)]
        public async Task ConversationPushStream()
        {
            var config = CreateCTSInRoomSpeechConfig();
            var stopRecognition = new TaskCompletionSource<int>();
            string recoResult = string.Empty;

            using (var pushStream = AudioInputStream.CreatePushStream())
            {
                using (var audioInput = AudioConfig.FromStreamInput(pushStream))
                {
                    var meetingID = Guid.NewGuid().ToString();
                    using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
                    {
                        conversation.Properties.SetProperty("iCalUid", "040000008200E00074C5B7101A82E008000000001003D722197CD4010000000000000000100000009E970FDF583F9D4FB999E607891A2F66");

                        using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
                        {
                            await conversation.AddParticipantAsync("xyz@example.com");

                            conversationTranscriber.Transcribing += (s, e) =>
                            {
                                Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                            };

                            conversationTranscriber.Transcribed += (s, e) =>
                            {
                                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                                {
                                    Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                                    if (string.IsNullOrEmpty(recoResult))
                                    {
                                        recoResult = e.Result.Text;
                                    }
                                }
                                else if (e.Result.Reason == ResultReason.NoMatch)
                                {
                                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                                }
                            };

                            conversationTranscriber.Canceled += (s, e) =>
                            {
                                Console.WriteLine($"CANCELED: Reason={e.Reason}");

                                if (e.Reason == CancellationReason.Error)
                                {
                                    Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                                    Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                                }

                                stopRecognition.TrySetResult(0);
                            };

                            conversationTranscriber.SessionStarted += (s, e) =>
                            {
                                Console.WriteLine("\nSession started event.");
                            };

                            conversationTranscriber.SessionStopped += (s, e) =>
                            {
                                Console.WriteLine("\nSession stopped event.");
                                Console.WriteLine("\nStop recognition.");
                                stopRecognition.TrySetResult(0);
                            };

                            await conversationTranscriber.JoinConversationAsync(conversation);

                            // Starts continuous recognition. Uses StopTranscribingAsync() to stop transcribing.
                            await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                            // open and read the wave file and push the buffers into the recognizer
                            using (var reader = Util.CreateWavReader(AudioUtterancesMap[AudioUtteranceKeys.CONVERSATION_BETWEEN_TWO_PERSONS_ENGLISH].FilePath.GetRootRelativePath()))
                            {
                                byte[] buffer = new byte[3200];
                                while (true)
                                {
                                    var readSamples = reader.Read(buffer, (uint)buffer.Length);
                                    if (readSamples == 0)
                                    {
                                        break;
                                    }
                                    pushStream.Write(buffer, readSamples);
                                    pushStream.SetProperty(PropertyId.ConversationTranscribingService_DataBufferTimeStamp, "fakeTimeStamp");
                                    pushStream.SetProperty("DataChunk_SpeakerId", "fakeSpeakerId");
                                }
                            }
                            pushStream.Close();

                            // Waits for completion.
                            // Use Task.WaitAny to keep the task rooted.
                            Task.WaitAny(new[] { stopRecognition.Task });

                            await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
                            SPXTEST_ISFALSE(string.IsNullOrEmpty(recoResult));
                        }
                    }
                }
            }
        }

        [RetryTestMethod]
        [Ignore("Temporarily Disabled because enrollment service is broken, see bug #2027717")]
        public async Task ConversationPullStreamWithEnrollment()
        {
            var config = CreateCTSInRoomSpeechConfig();

            var stopRecognition = new TaskCompletionSource<int>();
            List<string> speakers = new List<string>();
            bool bGotReco = false;

            var voiceSignature = CreateVoiceSignatureFromVoiceSample(AudioUtterancesMap[AudioUtteranceKeys.PERSON_ENROLLMENT_ENGLISH_1].FilePath.GetRootRelativePath());
            var signatureKatie = JsonConvert.SerializeObject(voiceSignature.Result.Signature);
            voiceSignature = CreateVoiceSignatureFromVoiceSample(AudioUtterancesMap[AudioUtteranceKeys.PERSON_ENROLLMENT_ENGLISH_2].FilePath.GetRootRelativePath());
            var signatureSteve = JsonConvert.SerializeObject(voiceSignature.Result.Signature);

            using (var audioInput = Util.OpenWavFile(AudioUtterancesMap[AudioUtteranceKeys.CONVERSATION_BETWEEN_TWO_PERSONS_ENGLISH].FilePath.GetRootRelativePath()))
            {
                var meetingID = Guid.NewGuid().ToString();
                using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
                {
                    using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
                    {
                        conversationTranscriber.Transcribing += (s, e) =>
                        {
                            Console.WriteLine($"RECOGNIZING: Text={e.Result.Text} SpeakerId={e.Result.UserId}");
                        };

                        conversationTranscriber.Transcribed += (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                            {
                                Console.WriteLine($"RECOGNIZED: Text={e.Result.Text} SpeakerId={e.Result.UserId}");
                                if (!speakers.Contains(e.Result.UserId) && e.Result.UserId != "Unidentified")
                                {
                                    speakers.Add(e.Result.UserId);
                                }
                                bGotReco = true;
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                            }
                        };

                        conversationTranscriber.Canceled += (s, e) =>
                        {
                            Console.WriteLine($"CANCELED: Reason={e.Reason}");

                            if (e.Reason == CancellationReason.Error)
                            {
                                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                                Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                                Console.WriteLine($"CANCELED: Did you update the subscription info?");
                                stopRecognition.TrySetResult(0);
                            }
                        };

                        conversationTranscriber.SessionStarted += (s, e) =>
                        {
                            Console.WriteLine("\nSession started event.");
                        };

                        conversationTranscriber.SessionStopped += (s, e) =>
                        {
                            Console.WriteLine("\nSession stopped event.");
                            Console.WriteLine("\nStop recognition.");
                            stopRecognition.TrySetResult(0);
                        };

                        // Sets a conversation Id.


                        // Add participants to the conversation.
                        // Voice signature needs to be in the following format:
                        // { "Version": <Numeric value>, "Tag": "string", "Data": "string" }
                        var speakerA = Participant.From("Katie", Language.EN, signatureKatie);
                        var speakerB = Participant.From("Steve", Language.EN, signatureSteve);
                        await conversation.AddParticipantAsync(speakerA);
                        await conversation.AddParticipantAsync(speakerB);

                        await conversationTranscriber.JoinConversationAsync(conversation);
                        // Starts continuous recognition. Uses StopTranscribingAsync() to stop transcribing.
                        await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                        // Waits for completion.
                        // Use Task.WaitAny to keep the task rooted.
                        Task.WaitAny(new[] { stopRecognition.Task });

                        await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);

                        SPXTEST_ISTRUE(bGotReco);
                        SPXTEST_ISTRUE(speakers.Contains("Katie"));
                        SPXTEST_ISTRUE(speakers.Contains("Steve"));
                    }
                }
            }
        }

        [Ignore]
        [RetryTestMethod]
        public async Task ConversationDefaultLanguageAndDetailedOutput()
        {
            var config = CreateCTSInRoomSpeechConfig();
            config.OutputFormat = OutputFormat.Detailed;
            var audioInput = AudioConfig.FromWavFileInput(AudioUtterancesMap[AudioUtteranceKeys.CONVERSATION_BETWEEN_TWO_PERSONS_ENGLISH].FilePath.GetRootRelativePath());
            var meetingID = Guid.NewGuid().ToString();
            using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
            {
                using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
                {
                    await conversationTranscriber.JoinConversationAsync(conversation);
                    var recoLanguage = conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_RecoLanguage);
                    SPXTEST_ISTRUE(String.IsNullOrEmpty(recoLanguage), "RecoLanguage should not be set here. RecoLanguage: " + recoLanguage);


                    await helper.CompleteContinuousRecognition(conversationTranscriber, meetingID);
                    var connectionUrl = conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_Url);
                    // Currently we do not have endpoint ready that supports detailed conversation transcription, so we only check connection URL for now.
                    SPXTEST_ISTRUE(connectionUrl.Contains("format=detailed"), "mismatch initialSilencetimeout in " + connectionUrl);
                    SPXTEST_ISTRUE(connectionUrl.Contains("language=en-us"), "Incorrect default language (should be en-us) in " + connectionUrl);
                }
            }
        }

        [TestMethod, TestCategory(TestCategory.LongRunning)]
        public async Task CreateVoiceSignature()
        {
            var result = await CreateVoiceSignatureFromVoiceSample(AudioUtterancesMap[AudioUtteranceKeys.PERSON_ENROLLMENT_ENGLISH_1].FilePath.GetRootRelativePath());
            SPXTEST_ISTRUE(result.Status == "OK", "Voice signature is not valid");
        }

        private async Task<VoiceSignature> CreateVoiceSignatureFromVoiceSample(string voiceSample)
        {
            byte[] fileBytes = File.ReadAllBytes(voiceSample);
            var content = new ByteArrayContent(fileBytes);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", conversationTranscriptionPPEKey);
            var response = await client.PostAsync($"https://signature.princetonppe.customspeech.ai/api/v1/Signature/GenerateVoiceSignatureFromByteArray", content);
            SPXTEST_ISTRUE(response.IsSuccessStatusCode, $"CreateVoiceSignatureFromVoiceSample failed to get a response from the enrolment service. HTTP_Status={response.StatusCode}");
            var jsonData = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"CreateVoiceSignatureFromVoiceSample: Service Response jsonData={jsonData}");
            var result = JsonConvert.DeserializeObject<VoiceSignature>(jsonData);
            return result;
        }

        private SpeechConfig CreateCTSInRoomSpeechConfig()
        {
            var config = SpeechConfig.FromSubscription(conversationTranscriptionPRODKey, conversationTranscriptionPRODRegion);
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            return config;
        }

        private SpeechConfig CreateCTSTeamsSpeechConfig(string endpointKey, string extraParams = "")
        {
            var config = SpeechConfig.FromEndpoint(new Uri(endpointKey + extraParams), conversationTranscriptionPPEKey);
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            return config;
        }

        [RetryTestMethod]
        public async Task TestDelayedAudio()
        {
            var config = CreateCTSTeamsSpeechConfig(DefaultSettingsMap[DefaultSettingKeys.ONLINE_AUDIO_ENDPOINT]);
            config.OutputFormat = OutputFormat.Detailed;

            ManualResetEvent transcribedEvent = new ManualResetEvent(false);
            ManualResetEvent speechStartEvent = new ManualResetEvent(false);
            ManualResetEvent speechEndEvent = new ManualResetEvent(false);
            ManualResetEvent canceledEvent = new ManualResetEvent(false);

            var pushStream = AudioInputStream.CreatePushStream();
            var audioInput = AudioConfig.FromStreamInput(pushStream);
            var meetingID = Guid.NewGuid().ToString();

            using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
            using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
            {
                await conversationTranscriber.JoinConversationAsync(conversation);
                var recoLanguage = conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_RecoLanguage);
                SPXTEST_ISTRUE(String.IsNullOrEmpty(recoLanguage), "RecoLanguage should not be set here. RecoLanguage: " + recoLanguage);

                conversationTranscriber.Transcribed += (s, e) =>
                {
                    Console.WriteLine($"Recognized {e.Result.Text}");
                    transcribedEvent.Set();
                };

                conversationTranscriber.SpeechStartDetected += (s, e) =>
                {
                    Console.WriteLine("Speech Started");
                    speechStartEvent.Set();
                };

                conversationTranscriber.SpeechEndDetected += (s, e) =>
                {
                    Console.WriteLine("Speech Ended");
                    speechEndEvent.Set();
                };

                conversationTranscriber.Canceled += (s, e) =>
                {
                    SPXTEST_ISTRUE(string.IsNullOrWhiteSpace(e.ErrorDetails), $"Error details were present {e.ErrorDetails}");
                    SPXTEST_ISTRUE(e.Reason == CancellationReason.EndOfStream, $"Cancellation Reason was not EOS it was {e.Reason.ToString()} with error code ${e.ErrorCode.ToString()}");
                    Console.WriteLine("Reached EndOfStream");
                    canceledEvent.Set();
                };

                var con = Connection.FromRecognizer(conversationTranscriber);
                con.MessageReceived += (s, e) =>
                {
                    Console.WriteLine($"Message Received {e.Message}");
                };

                await conversationTranscriber.StartTranscribingAsync();
                await conversation.AddParticipantAsync("userID@microsoft.com");

                await Task.Delay(TimeSpan.FromSeconds(5));

                var fileContents = File.ReadAllBytes(AudioUtterancesMap[AudioUtteranceKeys.SINGLE_UTTERANCE_ENGLISH].FilePath.GetRootRelativePath());

                pushStream.Write(fileContents);
                pushStream.Close();

                SPXTEST_ISTRUE(WaitHandle.WaitAll(new System.Threading.WaitHandle[] { transcribedEvent, canceledEvent },
                    TimeSpan.FromSeconds(3000)), "Events were not received in time");
            }
        }

        [RetryTestMethod]
        public async Task TestTempRedirect()
        {
            var config = CreateCTSTeamsSpeechConfig(DefaultSettingsMap[DefaultSettingKeys.RECONNECT_AUDIO_ENDPOINT], "?forceTemporaryRedirect=true");
            config.SetServiceProperty("maxConnectionDurationSecs", "5", ServicePropertyChannel.UriQueryParameter);

            ManualResetEvent reconnectEvent = new ManualResetEvent(false);
            ManualResetEvent canceledEvent = new ManualResetEvent(false);

            int successfulConnections = 0;

            var pushStream = AudioInputStream.CreatePushStream();
            var audioInput = AudioConfig.FromStreamInput(pushStream);
            var fileContents = File.ReadAllBytes(AudioUtterancesMap[AudioUtteranceKeys.MULTIPLE_UTTERANCE_ENGLISH].FilePath.GetRootRelativePath());

            pushStream.Write(fileContents);
            pushStream.Close();

            var meetingID = Guid.NewGuid().ToString();

            using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
            using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
            {
                await conversationTranscriber.JoinConversationAsync(conversation);
                var recoLanguage = conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_RecoLanguage);
                SPXTEST_ISTRUE(String.IsNullOrEmpty(recoLanguage), "RecoLanguage should not be set here. RecoLanguage: " + recoLanguage);

                conversationTranscriber.Transcribed += (s, e) =>
                {
                    Console.WriteLine($"Recognized {e.Result.Text}");
                };

                conversationTranscriber.Canceled += (s, e) =>
                {
                    SPXTEST_ISTRUE(string.IsNullOrWhiteSpace(e.ErrorDetails), $"Error details were present {e.ErrorDetails}");
                    SPXTEST_ISTRUE(e.Reason == CancellationReason.EndOfStream, $"Cancellation Reason was not EOS it was {e.Reason.ToString()} with error code ${e.ErrorCode.ToString()}");
                    Console.WriteLine("Reached EndOfStream");
                    canceledEvent.Set();
                };

                var con = Connection.FromRecognizer(conversationTranscriber);

                con.Connected += (s, e) =>
                {
                    Console.WriteLine("Connected");

                    SPXTEST_ISTRUE(conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_Url).ToLowerInvariant().Contains("forcetemporaryredirect=false"));
                    SPXTEST_ISTRUE(conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_Endpoint).ToLowerInvariant().Contains("forcetemporaryredirect=true"));
                    successfulConnections++;
                    if (successfulConnections == 2)
                    {
                        reconnectEvent.Set();
                    }
                };

                await conversationTranscriber.StartTranscribingAsync();

                SPXTEST_ISTRUE(reconnectEvent.WaitOne(TimeSpan.FromSeconds(60)), "Events were not received in time");
            }
        }

        [RetryTestMethod]
        public async Task TestPermRedirect()
        {
            var config = CreateCTSTeamsSpeechConfig(DefaultSettingsMap[DefaultSettingKeys.RECONNECT_AUDIO_ENDPOINT], "?forcePermanentRedirect=true");
            config.SetServiceProperty("maxConnectionDurationSecs", "5", ServicePropertyChannel.UriQueryParameter);

            ManualResetEvent reconnectEvent = new ManualResetEvent(false);
            ManualResetEvent canceledEvent = new ManualResetEvent(false);

            int successfulConnections = 0;

            var pushStream = AudioInputStream.CreatePushStream();
            var audioInput = AudioConfig.FromStreamInput(pushStream);
            var fileContents = File.ReadAllBytes(AudioUtterancesMap[AudioUtteranceKeys.MULTIPLE_UTTERANCE_ENGLISH].FilePath.GetRootRelativePath());

            pushStream.Write(fileContents);
            pushStream.Close();

            var meetingID = Guid.NewGuid().ToString();

            using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
            using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
            {
                await conversationTranscriber.JoinConversationAsync(conversation);
                var recoLanguage = conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_RecoLanguage);
                SPXTEST_ISTRUE(String.IsNullOrEmpty(recoLanguage), "RecoLanguage should not be set here. RecoLanguage: " + recoLanguage);

                conversationTranscriber.Transcribed += (s, e) =>
                {
                    Console.WriteLine($"Recognized {e.Result.Text}");
                };

                conversationTranscriber.Canceled += (s, e) =>
                {
                    SPXTEST_ISTRUE(string.IsNullOrWhiteSpace(e.ErrorDetails), $"Error details were present {e.ErrorDetails}");
                    SPXTEST_ISTRUE(e.Reason == CancellationReason.EndOfStream, $"Cancellation Reason was not EOS it was {e.Reason.ToString()} with error code ${e.ErrorCode.ToString()}");
                    Console.WriteLine("Reached EndOfStream");
                    canceledEvent.Set();
                };

                var con = Connection.FromRecognizer(conversationTranscriber);

                con.Connected += (s, e) =>
                {
                    Console.WriteLine("Connected");

                    SPXTEST_ISTRUE(conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_Url).ToLowerInvariant().Contains("forcepermanentredirect=false"), conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_Url));
                    SPXTEST_ISTRUE(conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_Endpoint).ToLowerInvariant().Contains("forcepermanentredirect=false"), conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_Endpoint));
                    successfulConnections++;
                    if (successfulConnections == 2)
                    {
                        reconnectEvent.Set();
                    }
                };

                await conversationTranscriber.StartTranscribingAsync();

                SPXTEST_ISTRUE(reconnectEvent.WaitOne(TimeSpan.FromSeconds(60)), "Events were not received in time");
            }
        }

        [RetryTestMethod]
        public async Task TestTooManyParticipants()
        {
            var config = CreateCTSTeamsSpeechConfig(DefaultSettingsMap[DefaultSettingKeys.ONLINE_AUDIO_ENDPOINT]);
            config.OutputFormat = OutputFormat.Detailed;

            var pushStream = AudioInputStream.CreatePushStream();
            var audioInput = AudioConfig.FromStreamInput(pushStream);
            var meetingID = Guid.NewGuid().ToString();

            config.SetProperty("Conversation-MaximumAllowedParticipants", "100");

            using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
            using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
            {
                await conversationTranscriber.JoinConversationAsync(conversation);
                var recoLanguage = conversationTranscriber.Properties.GetProperty(PropertyId.SpeechServiceConnection_RecoLanguage);
                SPXTEST_ISTRUE(String.IsNullOrEmpty(recoLanguage), "RecoLanguage should not be set here. RecoLanguage: " + recoLanguage);

                bool threw = false;
                int participants = 0;
                while (!threw)
                {
                    try
                    {
                        await conversation.AddParticipantAsync(Guid.NewGuid().ToString());
                        ++participants;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        threw = true;
                        SPXTEST_ISTRUE(participants == 100);
                    }
                }
            }
        }

        // This test covers sending a binary message in Rockfall project. Don't turn it off.
        [RetryTestMethod]
        public async Task TestSendMessageAsync()
        {
            var config = CreateCTSInRoomSpeechConfig();
            //config.SetProperty(PropertyId.Speech_LogFilename, "sendMessageSync_log.txt");
            //config.SetProxy("127.0.0.1", 8888);

            var meetingID = Guid.NewGuid().ToString();

            var audioInput = AudioConfig.FromWavFileInput(AudioUtterancesMap[AudioUtteranceKeys.SINGLE_UTTERANCE_ENGLISH].FilePath.GetRootRelativePath());
            using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
            using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
            {
                await conversationTranscriber.JoinConversationAsync(conversation);

                var connection = Connection.FromRecognizer(conversationTranscriber);
                connection.MessageReceived += (s, e) =>
                {
                    Console.WriteLine($"Message Received {e.Message}");
                };

                // Sending some binary data so that we can verify we can send binary data out.
                var fileContents = File.ReadAllBytes(AudioUtterancesMap[AudioUtteranceKeys.SINGLE_UTTERANCE_ENGLISH].FilePath.GetRootRelativePath());
                bool threw = false;
                try
                {
                    const string messageName = "speech.datachannel";
                    await connection.SendMessageAsync(messageName, fileContents, (uint)fileContents.Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Catch exception: {e.Message}");
                    threw = true;
                }
                SPXTEST_ISFALSE(threw);
            }
        }


        // Uses th CTS endpoint as it suppports controlling the audio connection time.
        [RetryTestMethod]
        public async Task TestReconnect()
        {
            var config = CreateCTSInRoomSpeechConfig();
            config.OutputFormat = OutputFormat.Detailed;
            config.SetServiceProperty("maxConnectionDurationSecs", "15", ServicePropertyChannel.UriQueryParameter);
            config.SetProperty("SPEECH-AudioThrottleAsPercentageOfRealTime", "100");
            
            ContinuousFilePullStream inputStream = new ContinuousFilePullStream(AudioUtterancesMap[AudioUtteranceKeys.CONVERSATION_BETWEEN_TWO_PERSONS_ENGLISH].FilePath.GetRootRelativePath());

            var audioInput = AudioConfig.FromStreamInput(inputStream, AudioStreamFormat.GetWaveFormatPCM(16000, 16, 8));
            var meetingID = Guid.NewGuid().ToString();
            int connectedCount = 0;
            int disconnectedCount = 0;
            var phrases = new Dictionary<int, string>();

            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

            Log("Creating conversation");
            using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
            {
                Log("Creating transcriber");
                using (var conversationTranscriber = TrackSessionId(new ConversationTranscriber(audioInput)))
                {
                    Log("Joining conversation");
                    await conversationTranscriber.JoinConversationAsync(conversation);

                    Log("Getting connection");
                    var con = Connection.FromRecognizer(conversationTranscriber);

                    Log("Setting up connection events");
                    con.Connected += (s, e) =>
                    {
                        Log($"Connected {connectedCount++}");
                    };
                    con.Disconnected += (s, e) =>
                    {
                        Log($"Disconnected {disconnectedCount}");
                        if (disconnectedCount++ > 3)
                        {
                            taskCompletionSource.SetResult(true);
                        }
                    };

                    string canceled = string.Empty;

                    Log("Setting up transcriber events");
                    conversationTranscriber.SessionStopped += (s, e) =>
                    {
                        Log("Session Stopped");
                        taskCompletionSource.TrySetResult(false);
                    };

                    conversationTranscriber.Canceled += (s, e) =>
                    {
                        canceled = e.ErrorDetails;
                        if (e.Reason == CancellationReason.Error)
                        {
                            taskCompletionSource.TrySetResult(false);
                        }
                    };

                    conversationTranscriber.Transcribed += (s, e) =>
                    {
                        Log($"Transcribed {e.Result.Text}");

                        if (!phrases.ContainsKey(connectedCount))
                        {
                            phrases.Add(connectedCount, "");
                        }

                        phrases[connectedCount] += e.Result.Text;
                    };

                    Log("Starting transcription");
                    await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                    Log("Waiting on task");

                    if (await Task.WhenAny(taskCompletionSource.Task, Task.Delay(TimeSpan.FromMinutes(3))) == taskCompletionSource.Task)
                    {
                        SPXTEST_REQUIRE(await taskCompletionSource.Task, $"Canceled unexpecdedly {canceled}");
                    }
                    else
                    {
                        SPXTEST_REQUIRE(false, "Test timed out");
                    }

                    Log("Stopping transcription");
                    await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);

                    GC.KeepAlive(conversation);
                    GC.KeepAlive(con);

                    Log("Verifying results");
                    for (int i = 1; i < disconnectedCount; i++)
                    {
                        SPXTEST_REQUIRE(!string.IsNullOrEmpty(phrases[i]), $"Connection {i} had no non-empty phrases");
                    }

                    SPXTEST_ISTRUE(connectedCount > 2, $"Connected count {connectedCount} was not > 2");
                    SPXTEST_ISTRUE(disconnectedCount > 2, $"Connected count {disconnectedCount} was not > 2");

                }
            }
        }

    }


}
