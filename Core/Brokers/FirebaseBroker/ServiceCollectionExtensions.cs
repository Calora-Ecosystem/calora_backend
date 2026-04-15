using Core.Brokers.GeminiBroker.Contracts;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Google.GenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.Brokers.FirebaseBroker;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFirebaseAdmin(this IServiceCollection services)
    {
        services.AddSingleton(provider => FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile("Resources/calora-google.json")
        }));
        services.AddSingleton(provider => FirebaseMessaging.GetMessaging(provider.GetRequiredService<FirebaseApp>()));
        
        return services;
    }
}