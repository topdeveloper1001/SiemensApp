// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const objectIdPropertyName = "ObjectId";
const defaultPropertyName = "DefaultProperty";
const retrievePropertyNames = ["FunctionDefaultProperty"];

function onError(e) {
  this.cancelChanges();

  var errorMessage = "";
  if (e.errors) {
    for (var error in e.errors) {
      if (e.errors[error].errors && e.errors[error].errors.length > 0) {
        errorMessage += e.errors[error].errors[0] + " ";
      }
    }
  }

  alert(errorMessage);
}

async function onChange(e) {
  $("#attributesTable tbody").html("");
  $("#propertiesTable tbody").html("");
  $("#functionPropertiesTable tbody").html("");

  let selection = e.sender.select();

  if (selection.length <= 0) {
    return;
  }

  let dataItem = e.sender.dataItem(selection);

  if (dataItem.Attributes) {

    let attributes = JSON.parse(dataItem.Attributes);
    let retrievedPropertyValues = {};
    let objectId = attributes[objectIdPropertyName];
    if (objectId) {
      let keys = Object.keys(attributes);

      let retrievedValuePromises = {};
      let retrieveKeys = keys.filter(k => k === defaultPropertyName || retrievePropertyNames.indexOf(k) >= 0);
      for (let i = 0; i < retrieveKeys.length; i++) {
        let key = retrieveKeys[i];
        let val = attributes[key];
        retrievedValuePromises[key] = key === defaultPropertyName
          ? getPropertyValue(objectId)
          : getPropertyValue(objectId, val);
      }

      for (let i = 0; i < retrieveKeys.length; i++) {
        let key = retrieveKeys[i];
        retrievedPropertyValues[key] = await retrievedValuePromises[key];
      }
    }

    Object.keys(attributes).forEach(function(key) {
      let retrievedValue = retrievedPropertyValues[key] || "";
      $(`<tr>
        <td>${key}</td>
        <td>${attributes[key]}</td>
        <td>${retrievedValue}</td>
        </tr>`)
        .appendTo("#attributesTable tbody");
    });
  }

  if (dataItem.Properties) {
    let properties = JSON.parse(dataItem.Properties);
    for (let i = 0; i < properties.length; i++) {
      $(`<tr>
        <td>${properties[i]}</td>
        </tr>`)
        .appendTo("#propertiesTable tbody");
    }
    
  }

  if (dataItem.FunctionProperties) {
    let properties = JSON.parse(dataItem.FunctionProperties);
    for (let i = 0; i < properties.length; i++) {
      $(`<tr>
        <td>${properties[i]}</td>
        </tr>`)
        .appendTo("#functionPropertiesTable tbody");
    }
  }
}

async function getPropertyValue(objectId, propertyName) {
  let url = `/api/propertyValues/${objectId}`;
  if (propertyName) {
    url += `/${propertyName}`;
  }

  try {
    let response = await $.getJSON(url);
    return response.Value.Value || "";
  } catch (err) {
    return "";
  }

}