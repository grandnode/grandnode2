function toggleMongoDBConnectionInfo() {
    if (document.getElementById('MongoDBConnectionInfo').checked == true) {
        document.getElementById('MongoDBDatabaseConnectionString').style.display = "flex";
        document.getElementById('MongoDBSimpleData').style.display = "none";
        var child = document.getElementById('collation');
        document.getElementById('MongoDBDatabaseConnectionString').appendChild(child);

    } else {
        document.getElementById('MongoDBDatabaseConnectionString').style.display = "none";
        document.getElementById('MongoDBSimpleData').style.display = "block";
        var child = document.getElementById('collation');
        document.querySelector('.mongoDBDatabaseName').appendChild(child);
    }
}
document.addEventListener('DOMContentLoaded', function () {
    if (document.getElementById('installation')) {
        document.getElementById('installation').addEventListener("click", function () {
            document.querySelector(".throbber").style.display = "block";
            window.setTimeout(function () {
                document.getElementById('installation-form').submit();
                document.getElementById('installation-form').setAttribute('disabled', 'disabled');
            }, 10);
        });
    }
    if (document.getElementById('MongoDBConnectionInfo')) {
        toggleMongoDBConnectionInfo();
    }
});