function reviewHelpfullness(element) {
    var productId = element.dataset.id;
    var reviewId = element.dataset.reviewid;
    var toastTitle = element.dataset.title;
    var url = element.dataset.url;

    document.getElementById('vote-yes-'+ reviewId +'').addEventListener("click", function (e) {
        setProductReviewHelpfulness(url, 'true');
    });
    document.getElementById('vote-no-'+ reviewId +'').addEventListener("click", function (e) {
        setProductReviewHelpfulness(url, 'false');
    });

    function setProductReviewHelpfulness(url, wasHelpful) {
        axios({
            url: url,
            method: 'post',
            params: { "productReviewId": reviewId, "productId": productId, "washelpful": wasHelpful }
        }).then(function (response) {
            document.getElementById("helpfulness-vote-yes-" + reviewId + "").innerHTML = response.data.TotalYes
            document.getElementById("helpfulness-vote-no-" + reviewId + "").innerHTML = response.data.TotalNo;
            new Vue({
                el: ".modal-place",
                methods: {
                    toast() {
                        this.$bvToast.toast(response.data.Result, {
                            title: toastTitle,
                            variant: 'info',
                            autoHideDelay: 3000,
                            solid: true
                        })
                    }
                },
                mounted: function () {
                    this.toast();
                }
            });
        }).catch(function (error) {
            alert(error);
        })
    }   
}

document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("#product-review-list .product-review-item").forEach(function (element) {
        reviewHelpfullness(element);
    });
})