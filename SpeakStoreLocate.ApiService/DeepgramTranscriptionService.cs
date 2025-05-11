using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Deepgram;
using Deepgram.Clients.Interfaces.v1;
using Deepgram;
using Deepgram.Clients.Interfaces.v1;
using Deepgram.Models.PreRecorded.v1;

namespace SpeakStoreLocate.ApiService;

public class DeepgramTranscriptionService : ITranscriptionService
{
    private readonly IListenRESTClient deepgramClient;
    private readonly AmazonS3Client _s3Client;
    private readonly string _bucketName;

    public DeepgramTranscriptionService(IConfiguration configuration)
    {
        // Set "DEEPGRAM_API_KEY" environment variable to your Deepgram API Key

        this.deepgramClient = ClientFactory.CreateListenRESTClient();

        var awsOptions = configuration.GetSection("AWS");
        var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(awsOptions["AccessKey"], awsOptions["SecretKey"]);
        var awsRegion = Amazon.RegionEndpoint.GetBySystemName(awsOptions["Region"]);


        this._bucketName = configuration["AWS:BucketName"];

        var s3Config = new AmazonS3Config { RegionEndpoint = awsRegion };
        _s3Client = new AmazonS3Client(awsCredentials, s3Config);
    }

    public async Task<string> TranscriptAudioAsync(AudioUploadRequest request)
    {
        // 1) Datei in S3 hochladen
        var fileTransferUtility = new TransferUtility(_s3Client);
        string filename = "speak-store-locate-request-" + Guid.NewGuid().ToString();

        using (var ms = new MemoryStream())
        {
            await request.AudioFile.CopyToAsync(ms);
            ms.Position = 0;
            await fileTransferUtility.UploadAsync(ms, _bucketName, filename);
        }

        // 2) PreSigned URL erstellen (5 Minuten gültig)
        var presignRequest = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = filename,
            Expires = DateTime.UtcNow.AddMinutes(5)
        };

        string presignedUrl = _s3Client.GetPreSignedURL(presignRequest);

        var response = await deepgramClient.TranscribeUrl(
            new UrlSource(presignedUrl),
            new PreRecordedSchema()
            {
                Model = "nova-2",
                Punctuate = true,
                Language = "de",
            });

        // 1. Für jeden Kanal die beste Alternative herausholen
        var bestTranscriptionPerChannel = response
            .Results
            .Channels
            .Select(channel =>
                channel.Alternatives
                    .OrderByDescending(alt => alt.Confidence)
                    .First()
                    .Transcript
            );

        // 2. Alle Kanal-Transkripte zu einem Gesamtstring verbinden
        string transcriptText = string.Join(" ", bestTranscriptionPerChannel);


        return transcriptText;
    }
}