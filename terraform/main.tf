provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "proan-devops" {
  name     = "proan-devops-resources"
  location = "Central US"
}

resource "azurerm_virtual_network" "proan-devops" {
  name                = "proan-devops-network-001"
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.proan-devops.location
  resource_group_name = azurerm_resource_group.proan-devops.name
}

resource "azurerm_subnet" "proan-devops" {
  name                 = "internal"
  resource_group_name  = azurerm_resource_group.proan-devops.name
  virtual_network_name = azurerm_virtual_network.proan-devops.name
  address_prefixes     = ["10.0.2.0/24"]

  depends_on = [ azurerm_virtual_network.proan-devops ]
}

resource "azurerm_public_ip" "proan-devops" {
  name                = "proan-devops-pip"
  location            = azurerm_resource_group.proan-devops.location
  resource_group_name = azurerm_resource_group.proan-devops.name
  allocation_method   = "Static"
  sku                 = "Basic"
}

resource "azurerm_network_interface" "proan-devops" {
  name                = "proan-devops-nic"
  location            = azurerm_resource_group.proan-devops.location
  resource_group_name = azurerm_resource_group.proan-devops.name

  ip_configuration {
    name                          = "internal"
    subnet_id                     = azurerm_subnet.proan-devops.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.proan-devops.id
  }
}

resource "azurerm_windows_virtual_machine" "proan-devops" {
  name                = "vm-devops-01"
  resource_group_name = azurerm_resource_group.proan-devops.name
  location            = azurerm_resource_group.proan-devops.location
  size                = "Standard_A4_v2"
  admin_username      = "adminuser"
  admin_password      = "ZSQNeN0h86Rrhd"
  network_interface_ids = [
    azurerm_network_interface.proan-devops.id,
  ]

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }

  source_image_reference {
    publisher = "MicrosoftWindowsServer"
    offer     = "WindowsServer"
    sku       = "2016-Datacenter"
    version   = "latest"
  }
}

