import { send } from "../utilities";

var messageInput = document.querySelector("#messageInput") as HTMLInputElement;
var sendButton = document.querySelector("#sendButton") as HTMLButtonElement;
var responseDiv = document.querySelector("#responseDiv") as HTMLDivElement;

sendButton.onclick = async () => {
  var response = await send("message", messageInput.value);

  responseDiv.innerText = response;
};
