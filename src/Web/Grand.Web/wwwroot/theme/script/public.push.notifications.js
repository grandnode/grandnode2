var PushNotifications = {
    url: "",
    SenderId: "",
    ApiKey: "",
    AuthDomain: "",
    DatabaseUrl: "",
    ProjectId: "",
    StorageBucket: "",
    AppId: "",

    init: function init(ApiKey, SenderId, ProjectId, AuthDomain, StorageBucket, DatabaseUrl, url, appId) {
        this.url = url;
        this.SenderId = SenderId;
        this.ApiKey = ApiKey;
        this.AuthDomain = AuthDomain;
        this.DatabaseUrl = DatabaseUrl;
        this.ProjectId = ProjectId;
        this.StorageBucket = StorageBucket;
        this.AppId = appId;
    },

    process: function process() {

        var config = {
            apiKey: this.ApiKey,
            authDomain: this.AuthDomain,
            databaseURL: this.DatabaseUrl,
            projectId: this.ProjectId,
            storageBucket: this.StorageBucket,
            messagingSenderId: this.SenderId,
            appId: this.AppId
        };

        firebase.initializeApp(config);

        const messaging = firebase.messaging();
        var url = this.url;

        messaging.requestPermission()
            .then(function () {
                let success = false;
                let value = "";

                messaging.getToken()
                    .then(function (currentToken) {
                        if (currentToken != null) {
                            success = true;
                            value = currentToken;
                        } else {
                            success = false;
                            value = 'No Instance ID token available. Request permission to generate one';
                        }
                    })
                    .catch(function (err) {
                        success = false;
                        value = 'An error occurred while retrieving token. ' + err;
                    })
                    .finally(function () {

                        var bodyFormData = new FormData();
                        bodyFormData.append('success', success);
                        bodyFormData.append('value', value);

                        axios({
                            url: url,
                            data: bodyFormData,
                            method: 'post'
                        })
                    });
            })
            .catch(function (err) {
                var bodyFormData = new FormData();
                bodyFormData.append('success', success);
                bodyFormData.append('value', value);
                axios({
                    url: url,
                    data: bodyFormData,
                    method: 'post'
                })
            });

        messaging.onMessage(function (payload) {
            console.log(payload);

            const notificationTitle = payload.notification.title;
            const notificationOptions = {
                body: payload.notification.body,
                icon: payload.notification.icon
            };

            var notification = new Notification(notificationTitle, notificationOptions);
        });
    }
}