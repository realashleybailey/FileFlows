document.addEventListener("DOMContentLoaded", function()
{

});

function toggleCollapse(event){
    let id = event.target.id;
    console.log('event.target.id: ', event.target.id);
    console.log('event.target.checked: ', event.target.checked);
    localStorage.setItem('collapse_' + id, event.target.checked);

}