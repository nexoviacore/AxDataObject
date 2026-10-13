var pageURL = "../pages/OPEMR-PatientList";

function loadHTML() {
	$.ajax({
		type: "GET",
		url: pageURL,
		headers: {"Authorization": localStorage.getItem('RCP_JwtToken')},
		cache: false,
		async: true,
		dataType: "html",
		success: function (data) {
			//console.log(data);
			try {
				$("#divHome").html(data);
				if (data.indexOf("login2.js") > -1) {
					$("body .bgloader.mainloader").fadeOut(300, function () {
						$("body .bgloader.mainloader").remove();
					});
				}
			}
			catch (ex) {
				console.log('Error' + ex);
			}			
			//document.getElementById("divHome").innerHTML += data;
		},
		error: function (request, status, error) {
			//console.log(request);
			alert("User session has expired. Please re-login to continue.");
			localStorage.removeItem("RCP_JwtToken");
			window.location.href = "login";
		}
	});
}

$(document).ready(function () {
	loadHTML();
});