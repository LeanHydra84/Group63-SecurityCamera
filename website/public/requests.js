let url = "http://localhost:5251"


function parseCookie(cname)
{
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for(let i = 0; i <ca.length; i++) {
      let c = ca[i];
      while (c.charAt(0) == ' ') {
        c = c.substring(1);
      }
      if (c.indexOf(name) == 0) {
        return c.substring(name.length, c.length);
      }
    }
    return "";
}

function setCookie(name, value, days)
{
    var cookie = name + "=" + value
    var expires = new Date()
    expires.setDate(expires.getDate() + days)

    if (days)
    {
        cookie += "; expires=" + expires.toUTCString()
    }

    document.cookie = cookie
}

function deleteCookie(cname)
{
    var date = new Date()
    date.setTime(date.getTime() - 1)
    document.cookie = cname + "=; expires=" + date.toUTCString()
}

function deleteAuthenticationCookie()
{
    deleteCookie("username")
    deleteCookie("authentication")
}

function setAuthenticationCookie(obj)
{
    deleteAuthenticationCookie()
    setCookie("username", obj.username, 7)
    setCookie("authentication", obj.authentication, 7)
}

function doesCookieExist()
{
    if (parseCookie("username") === "") return false;
    if (parseCookie("authentication") === "") return false;
    return true
}

function createAuthHeaders()
{
    let username = parseCookie("username")
    let auth = parseCookie("authentication")

    return {
        "username" : username,
        "authentication" : auth,
        "Content-type": "application/json; charset=UTF-8"
    }
}

function createLoginHeaders(username, password)
{
    return {
        "username" : username,
        "password" : password,
        "Content-type": "application/json; charset=UTF-8"
    }
}

function postRequest(endpoint, headers, body={})
{
    let url = base_url + endpoint
    return fetch(
        url,
        {
            method : "POST",
            headers : headers,
            body : JSON.stringify(body)
        }
    )
}

function getRequest(endpoint, headers)
{
    let url = base_url + endpoint
    return fetch(
        url,
        {
            method : "GET",
            headers : headers,
        }
    )
}

function login(username, password)
{
    return postRequest("/login", JSON.stringify({ 'username': username, 'password': password }))
}

function createaccount(username, password)
{
    return postRequest("/newuser", JSON.stringify({ 'username': username, 'password': password }), "")
}