var popupParams = new Object();

function loadHTML(pageURL) {
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
				if (data.indexOf("login2.js") > -1) {
					alert("User session has expired. Please re-login to continue.");
					localStorage.removeItem("RCP_JwtToken");
					window.parent.location.href = "login";
					return;
				}

				$("#divHome").html(data);			
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
			window.parent.location.href = "login";
		}
	});
}

$(document).ready(function () {

	const params = new Proxy(new URLSearchParams(window.location.search), {
		get: (searchParams, prop) => searchParams.get(prop),
	})
	popupParams = params;
	let value = params.load;
	let pageURL = "../pages/" + value;

	loadHTML(pageURL);
});