import { send } from "../utilities";
import { Product } from "./types";

var addButton = document.querySelector("#addButton") as HTMLButtonElement;
var nameInput = document.querySelector("#nameInput") as HTMLInputElement;
var priceInput = document.querySelector("#priceInput") as HTMLInputElement;
var productsDiv = document.querySelector("#productsDiv") as HTMLUListElement;

var products = await send("getProducts") as Product[];

for (var i = 0; i < products.length; i++) {
  var product = products[i];

  var productLi = document.createElement("li");
  productLi.innerText = product.price + "₪ - " + product.name;

  productsDiv.appendChild(productLi);
}


addButton.onclick = async () => {
  var name = nameInput.value;
  var price = Number(priceInput.value);
  console.log(name, price);
  await send("addProduct", [name, price]);
  location.reload();
};
