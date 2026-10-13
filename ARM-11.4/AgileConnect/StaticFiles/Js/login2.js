var loginURL = "../api/Auth/login";

function Login(username, password, groupid) {
    this.username = username;
    this.password = password;
    this.groupid = groupid;
  }

function CallLoginAPI(){

	var loginObj = new Login($("#username").val(), MD5($("#password").val()), $('#usergroup option:selected').val());

	$.ajax({
		type: "POST",
		url: loginURL,
		cache: false,
		async: true,
		contentType: "application/json;charset=utf-8",
		data: JSON.stringify(loginObj),
		dataType: "text",
		success: function (token) {
			//console.log(token);
			localStorage.setItem("RCP_JwtToken", "Bearer " + token);
			localStorage.setItem("AxpertConnectUser", $("#username").val());
			window.location.href = "home";
		},
		error: function (request, status, error) {
			alert("Invalid username/password.");
		}
	});
}

function setMobileViewFlag() {
	if ($(window).width() < 450) {
		localStorage.setItem("RCP_MobileView", "true");
	}
	else {
		localStorage.setItem("RCP_MobileView", "false");
	}
}

$(document).ready(function () {
	$('.toggle-password').click(function () {
		$(this).children().toggleClass('fa-eye-slash fa-eye');
		let input = $(this).prev();
		input.attr('type', input.attr('type') === 'password' ? 'text' : 'password');
	});

	$("#login").click(function () {
		CallLoginAPI();
	});

	localStorage.removeItem("RCP_JwtToken");
});