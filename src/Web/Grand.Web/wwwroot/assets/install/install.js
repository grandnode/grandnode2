function toggleMongoDBConnectionInfo() {
    if (document.getElementById('ConnectionInfo').checked == true) {
        document.getElementById('DatabaseConnectionString').style.display = "flex";
        document.getElementById('DBSampleData').style.display = "none";
        var child = document.getElementById('collation');
        document.getElementById('DatabaseConnectionString').appendChild(child);
    } else {
        document.getElementById('DatabaseConnectionString').style.display = "none";
        document.getElementById('DBSampleData').style.display = "block";
        var child = document.getElementById('collation');
        document.querySelector('.mongoDBDatabaseName').appendChild(child);

        let dataProvider = document.getElementById('DataProvider');
        if (dataProvider.value != "0") {
            dataProvider.value = 0;
        }
    }
}
function DataProviderChange(provider) {
    if (provider != 0) {
        var elm = document.getElementById('ConnectionInfo');
        if (!elm.checked) {
            elm.click();
        }
    }
    else {
        var elm = document.getElementById('ConnectionInfo');
        if (elm.checked) {
            elm.click();
        }
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
    if (document.getElementById('ConnectionInfo')) {
        toggleMongoDBConnectionInfo();
    }
});