<h1>
  Docker Desktop/Elastic Install Guide for Windows
</h1>
<h2>
  Requirements
</h2>
<ul>
  <li>Windows 10 or 11</li>
  <li>About 20 GBs free space</li>
  <li>WSL support</li>
  <li>Ubuntu (from the Windows Store)</li>
  <li>Docker Desktop</li>
  <li>docker-compose.yml</li>
</ul>
<h2>
  Step 1: Install Requirements
</h2>
<li>
First, make sure you have enough free space to install all of the required components. You about 10 GBs of free storage to install the Elastic stack image in Docker Desktop and run the container. In addition to this, you need the required free space for Ubuntu and Docker Desktop, which is about 3 gigabytes. Make sure to have additional free space, as reaching a full hard drive during the download of Elastic will break your Docker Desktop.
  </li>
<li>
  Make sure that WSL is installed. In Windows 11, WSL should be enabled by default. In Windows 10, some older PCs might not have it installed. If you do not have it installed, you can run "wsl --install" in Windows PowerShell.
</li>
<li>
  Open the Windows Store and install the most recent version of Ubuntu. If you're running Windows 11, WSL should be enabled by default. If it is not, or if you are on Windows 10, you may need to enable Hardware Virtualization in your UEFI/BIOS. If Ubuntu does not start up, check to see if it's enabled.
</li>
<li>
  Now install Docker Desktop from their site and follow the installation instructions: https://www.docker.com/products/docker-desktop/
</li>
<h2>
  Step 2: Setting up the Docker Environment
</h2>
<li>
  Run Docker Desktop. Make sure you are running with WSL2 support if your PC supports it. To check what programs are running in WSL2, you can run the command "wsl --list --verbose" in Windows PowerShell. You want Ubuntu to be running with WSL Version 2. To set only Ubuntu to run in WSL2, run the command  "wsl --set-version Ubuntu 2" in PowerShell. To set everything to run using WSL2, you can run the command "wsl --set-default-version 2" in PowerShell.
</li>
<li>
  Make sure Docker Desktop is currently running Linux containers. In the Windows System Tray, right click Docker Desktop and look for the button that says "Switch to ....... containers". If this says "Switch to Linux Containers", press the option to do so. Otherwise, you may proceed to the next step.
</li>
<h2>
  Step 3: Verifying Ubuntu
</h2>
<li>
With Docker Desktop running, open an Ubuntu terminal by typing it in your Windows search bar. You should be located in your home folder. To test if everything is installed correctly, enter "docker-compose" into your Ubuntu terminal. If Docker Desktop is installed correctly and is currently running, you should be presented with a list of commands related to the docker-compose library. If not, verify that Docker Desktop is installed and running.
</li>
<h2>
  Step 4: Installing and Finding docker-compose.yml
</h2>
<li>
Find the docker-compose.yml file located in the "../StandUpApi/docker" folder. Note the entire file path to this file.
</li>
<li>
In the Ubuntu termal, navigate to the "../StandUpApi/docker" folder noted in the previous step. To do this, enter the command "cd /mnt/&lt;drive letter in lowercase&gt;/&lt;file path&gt;)" in your Ubuntu Terminal. The "/mnt/" points to your pc's local file system, since Ubuntu runs in a virtualized environment. In my case, my file is located in "C:/Users/pikal/source/repos/StandUpApi/docker", so I would enter "cd /mnt/c/Users/pikal/source/repos/StandUpApi/docker" in my Ubuntu terminal. You can make the docker-compose.yml to another folder to make it easier to access if you want to.
</li>
<h2>
  Step 5: Running docker-compose.yml
</h2>
<li>
In your Ubuntu terminal, which should be pointing to the folder where your docker-compose.yml is located, enter the command "docker-compose up -d". If the previous setup steps have been followed, you should see downloading bars in your Ubuntu terminal. Once these have finished and your Ubuntu terminal says Ready, then Elasticsearch and Kibana have been installed successfully. Make sure to leave the terminal open until you are ready to shut down Elasticsearch and Kibana so you don't have to enter the file path again.
</li>
<li>
To access Kibana, open a web browser, such as Firefox or Google Chrome, and enter "http://localhost:8200" into your url bar.
</li>
<h2>
  Step 6: Shutting down docker-compose.yml
</h2>
<li>When you want to shut down your docker-compose containers, go into your Ubuntu terminal, point it towards the docker-compose.yml file path, and enter the command "docker-compose down". This will shut down your containers and should save any data you have ingested into Elastic. Once that process is complete, you can now shut down Docker Desktop and your Ubuntu terminal.</li>
<li>If you want to run docker-compose.yml again, simply open Docker Desktop and Ubuntu, point Ubuntu towards the docker-compose.yml folder as in step 4, and type "docker-compose up -d". When you are done, you can close it with "docker compose down".</li>
