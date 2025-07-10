function toggleSubMenu(event) {
    event.preventDefault();
    const subMenu = document.getElementById("defaultSubMenu");
    const bootstrapCollapse = new bootstrap.Collapse(subMenu, {
        toggle: true
    });
    const icon = event.target.querySelector('.sidebar-icon');

}